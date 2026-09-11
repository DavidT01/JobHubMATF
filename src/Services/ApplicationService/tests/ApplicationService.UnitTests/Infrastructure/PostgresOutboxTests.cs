using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Data;
using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class PostgresOutboxTests
{
    [Fact]
    public async Task Migration_dispatch_retry_order_and_cross_instance_lock_work_on_postgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("JOBHUB_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Skip("Set JOBHUB_TEST_POSTGRES to an isolated PostgreSQL test database.");
        var schema = "outbox_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(connectionString);
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        var scopedConnection = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema }.ConnectionString;
        ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(scopedConnection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);
        try
        {
            var first = JobApplication.Create(Guid.NewGuid(), "candidate-one", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow.AddMinutes(-10));
            var firstEvent = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(first));
            first.ChangeStatus(ApplicationStatus.InReview, first.SubmittedAtUtc.AddMinutes(1));
            var secondEvent = OutboxMessage.Create(ApplicationLifecycleEvent.StatusChanged(first, ApplicationStatus.Submitted));
            var other = JobApplication.Create(Guid.NewGuid(), "candidate-two", "507f1f77bcf86cd799439012", null, DateTimeOffset.UtcNow.AddMinutes(-5));
            var otherEvent = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(other));
            await using (var db = Context())
            {
                await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
                db.JobApplications.AddRange(first, other);
                db.OutboxMessages.Add(firstEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.OutboxMessages.Add(secondEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.OutboxMessages.Add(otherEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                Assert.True(firstEvent.Sequence < secondEvent.Sequence && secondEvent.Sequence < otherEvent.Sequence);
            }

            var publisher = new Publisher();
            async Task<OutboxDispatchResult> Dispatch()
            {
                await using var db = Context();
                return await new OutboxDispatcher(db, new OutboxDelivery(publisher, TimeProvider.System,
                    NullLogger<OutboxDelivery>.Instance), TimeProvider.System).DispatchOneAsync(TestContext.Current.CancellationToken);
            }
            publisher.Fail = true;
            Assert.Equal(OutboxDispatchResult.RetryScheduled, await Dispatch());
            publisher.Fail = false;
            Assert.Equal(OutboxDispatchResult.Published, await Dispatch());
            Assert.Equal(otherEvent.Id, publisher.Ids[^1]); // Failed predecessor blocks only its own application.
            Assert.Equal(OutboxDispatchResult.NoWork, await Dispatch());

            await using (var db = Context())
            {
                var row = await db.OutboxMessages.SingleAsync(x => x.Id == firstEvent.Id, TestContext.Current.CancellationToken);
                Assert.Equal(1, row.Attempts);
                Assert.Null(row.PublishedAtUtc);
                Assert.Equal(firstEvent.Id, row.Id);
                await db.OutboxMessages.Where(x => x.Id == firstEvent.Id).ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.NextAttemptAtUtc, DateTimeOffset.UtcNow.AddSeconds(-1)), TestContext.Current.CancellationToken);
            }

            publisher.Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            publisher.Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var active = Dispatch();
            await publisher.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            try { Assert.Equal(OutboxDispatchResult.Busy, await Dispatch()); }
            finally { publisher.Release.TrySetResult(); }
            Assert.Equal(OutboxDispatchResult.Published, await active);
            publisher.Started = null;
            publisher.Release = null;
            Assert.Equal(OutboxDispatchResult.Published, await Dispatch());
            Assert.Equal(secondEvent.Id, publisher.Ids[^1]);
            Assert.Equal(OutboxDispatchResult.NoWork, await Dispatch());
            await using (var db = Context())
                Assert.Equal(3, await db.OutboxMessages.CountAsync(x => x.PublishedAtUtc != null, TestContext.Current.CancellationToken));
        }
        finally
        {
            // Exact schema created by this test; never drop the configured database.
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private sealed class Publisher : IOutboxPublisher
    {
        public bool Fail { get; set; }
        public List<Guid> Ids { get; } = [];
        public TaskCompletionSource? Started { get; set; }
        public TaskCompletionSource? Release { get; set; }
        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            Ids.Add(message.Id);
            if (Fail) throw new IOException("simulated broker failure");
            Started?.TrySetResult();
            if (Release is not null) await Release.Task.WaitAsync(cancellationToken);
        }
    }
}
