using System.Text;
using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class RabbitBrokerTests
{
    [Fact]
    public async Task Mandatory_return_fails_and_retry_reconnects_with_identical_persistent_payload()
    {
        var uri = Environment.GetEnvironmentVariable("JOBHUB_TEST_RABBITMQ");
        if (string.IsNullOrWhiteSpace(uri)) Assert.Skip("Set JOBHUB_TEST_RABBITMQ to an isolated RabbitMQ test broker.");
        var name = "outbox.test." + Guid.NewGuid().ToString("N");
        var options = new OutboxMessagingOptions { ConnectionUri = uri, Exchange = name };
        var factory = options.CreateConnectionFactory();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        var token = timeout.Token;
        await using var connection = await factory.CreateConnectionAsync(token);
        await using var admin = await connection.CreateChannelAsync(cancellationToken: token);
        var queueCreated = false;
        var exchangeCreated = false;
        try
        {
            await admin.ExchangeDeclareAsync(name, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: token);
            exchangeCreated = true;
            var row = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(JobApplication.Create(
                Guid.NewGuid(), "candidate", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow)));
            await using var publisher = new RabbitPublisherConnection(factory, options, NullLogger<RabbitPublisherConnection>.Instance);
            // Exchange exists but has no binding: a broker ack alone is not successful delivery.
            await Assert.ThrowsAsync<PublishReturnException>(() => publisher.PublishAsync(row, token));
            Assert.Null(row.PublishedAtUtc);

            await admin.QueueDeclareAsync(name, durable: true, exclusive: false, autoDelete: false, cancellationToken: token);
            queueCreated = true;
            await admin.QueueBindAsync(name, name, row.EventType, cancellationToken: token);
            // The previous failure disposed the publisher's session. This creates a fresh confirm channel.
            await publisher.PublishAsync(row, token);
            await publisher.PublishAsync(row, token);
            for (var copy = 0; copy < 2; copy++)
            {
                var received = await admin.BasicGetAsync(name, autoAck: true, cancellationToken: token);
                Assert.NotNull(received);
                Assert.Equal(row.Id.ToString(), received.BasicProperties.MessageId);
                Assert.Equal(row.ApplicationId.ToString(), received.BasicProperties.CorrelationId);
                Assert.Equal(row.EventType, received.RoutingKey);
                Assert.Equal(row.EventType, received.BasicProperties.Type);
                Assert.True(received.BasicProperties.Persistent);
                Assert.Equal("application/json", received.BasicProperties.ContentType);
                Assert.Equal("utf-8", received.BasicProperties.ContentEncoding);
                Assert.Equal(row.OccurredAtUtc.ToUnixTimeSeconds(), received.BasicProperties.Timestamp.UnixTime);
                Assert.Equal(row.Payload, Encoding.UTF8.GetString(received.Body.Span));
            }
            Assert.Null(await admin.BasicGetAsync(name, autoAck: true, cancellationToken: token));
            Assert.Null(row.PublishedAtUtc); // DB acknowledgement belongs to the dispatcher.
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            // Only the random queue/exchange created by this test are removed.
            if (queueCreated) await admin.QueueDeleteAsync(name, ifUnused: false, ifEmpty: false, cancellationToken: cleanup.Token);
            if (exchangeCreated) await admin.ExchangeDeleteAsync(name, ifUnused: false, cancellationToken: cleanup.Token);
        }
    }
}
