using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Data;
using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

[Collection("PostgreSQL outbox integration")]
public sealed class OutboxWorkerIntegrationTests
{
    [Fact]
    public async Task Registered_worker_delivers_and_resumes_pending_rows_after_host_restart()
    {
        var postgres = Environment.GetEnvironmentVariable("JOBHUB_TEST_POSTGRES");
        var rabbit = Environment.GetEnvironmentVariable("JOBHUB_TEST_RABBITMQ");
        if (string.IsNullOrWhiteSpace(postgres) || string.IsNullOrWhiteSpace(rabbit))
            Assert.Skip("Set both JOBHUB_TEST_POSTGRES and JOBHUB_TEST_RABBITMQ to isolated services.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var token = deadline.Token;
        var schema = "worker_test_" + Guid.NewGuid().ToString("N");
        var exchange = "outbox.worker.test." + Guid.NewGuid().ToString("N");
        await using var adminDb = new NpgsqlConnection(postgres);
        await adminDb.OpenAsync(token);
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", adminDb)) await create.ExecuteNonQueryAsync(token);
        var dbUri = new NpgsqlConnectionStringBuilder(postgres) { SearchPath = schema }.ConnectionString;
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(dbUri,
            options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options;
        var options = new OutboxMessagingOptions { ConnectionUri = rabbit, Exchange = exchange };
        await using var broker = await options.CreateConnectionFactory().CreateConnectionAsync(token);
        await using var channel = await broker.CreateChannelAsync(cancellationToken: token);
        var queueCreated = false;
        var exchangeCreated = false;
        try
        {
            await using (var db = new ApplicationDbContext(dbOptions)) await db.Database.MigrateAsync(token);
            await channel.ExchangeDeclareAsync(exchange, "topic", durable: true, autoDelete: false, cancellationToken: token);
            exchangeCreated = true;
            await channel.QueueDeclareAsync(exchange, durable: true, exclusive: false, autoDelete: false, cancellationToken: token);
            queueCreated = true;
            await channel.QueueBindAsync(exchange, exchange, "application.#", cancellationToken: token);

            async Task<Guid> Enqueue()
            {
                await using var db = new ApplicationDbContext(dbOptions);
                var application = JobApplication.Create(Guid.NewGuid(), "candidate", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow);
                var row = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(application));
                db.JobApplications.Add(application);
                db.OutboxMessages.Add(row);
                await db.SaveChangesAsync(token);
                return row.Id;
            }

            async Task RunHostUntilPublished(Guid eventId)
            {
                var builder = Host.CreateApplicationBuilder();
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Outbox:Enabled"] = "true", ["Outbox:ConnectionUri"] = rabbit, ["Outbox:Exchange"] = exchange
                });
                builder.Services.AddSingleton(TimeProvider.System);
                builder.Services.AddDbContext<ApplicationDbContext>(db => db.UseNpgsql(dbUri,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema)));
                builder.Services.AddOutboxMessaging(builder.Configuration);
                var host = builder.Build();
                try
                {
                    await host.StartAsync(token);
                    while (true)
                    {
                        await using var db = new ApplicationDbContext(dbOptions);
                        if (await db.OutboxMessages.AnyAsync(row => row.Id == eventId && row.PublishedAtUtc != null, token)) break;
                        await Task.Delay(100, token);
                    }
                }
                finally
                {
                    using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    try { await host.StopAsync(shutdown.Token); }
                    finally
                    {
                        if (host is IAsyncDisposable asyncHost) await asyncHost.DisposeAsync();
                        else host.Dispose();
                    }
                }
            }

            var firstId = await Enqueue();
            await RunHostUntilPublished(firstId);
            var first = await channel.BasicGetAsync(exchange, autoAck: true, cancellationToken: token);
            Assert.Equal(firstId.ToString(), first!.BasicProperties.MessageId);
            // Enqueue while no worker is running, then start a new host with production registrations.
            var secondId = await Enqueue();
            await RunHostUntilPublished(secondId);
            var second = await channel.BasicGetAsync(exchange, autoAck: true, cancellationToken: token);
            Assert.Equal(secondId.ToString(), second!.BasicProperties.MessageId);
            Assert.Null(await channel.BasicGetAsync(exchange, autoAck: true, cancellationToken: token));
            await using var finalDb = new ApplicationDbContext(dbOptions);
            Assert.Equal(2, await finalDb.OutboxMessages.CountAsync(row => row.PublishedAtUtc != null, token));
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                if (queueCreated) await channel.QueueDeleteAsync(exchange, false, false, cancellationToken: cleanup.Token);
                if (exchangeCreated) await channel.ExchangeDeleteAsync(exchange, false, cancellationToken: cleanup.Token);
            }
            finally
            {
                await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", adminDb);
                await drop.ExecuteNonQueryAsync(cleanup.Token);
            }
        }
    }
}
