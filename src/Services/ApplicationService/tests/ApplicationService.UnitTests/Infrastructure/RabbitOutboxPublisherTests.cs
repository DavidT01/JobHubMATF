using System.Text;
using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Outbox;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class RabbitOutboxPublisherTests
{
    private const string Exchange = "jobhub.applications.v1";
    private static OutboxMessage Message() => OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(
        JobApplication.Create(Guid.NewGuid(), "candidate", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow)));

    [Fact]
    public async Task Uses_confirm_tracking_and_durable_topic_and_preserves_message_on_retry()
    {
        var channel = new Mock<IChannel>();
        var connection = Connection(channel);
        var message = Message();
        var calls = 0;
        channel.Setup(x => x.BasicPublishAsync(Exchange, message.EventType, true, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>((_, _, _, properties, body, _) =>
            {
                calls++;
                Assert.True(properties.Persistent);
                Assert.Equal(message.Id.ToString(), properties.MessageId);
                Assert.Equal(message.ApplicationId.ToString(), properties.CorrelationId);
                Assert.Equal(message.EventType, properties.Type);
                Assert.Equal("application/json", properties.ContentType);
                Assert.Equal(message.Payload, Encoding.UTF8.GetString(body.Span));
            }).Returns(ValueTask.CompletedTask);
        await using var publisher = await RabbitOutboxPublisher.CreateAsync(connection.Object, Exchange, TestContext.Current.CancellationToken);
        await publisher.PublishAsync(message, TestContext.Current.CancellationToken);
        await publisher.PublishAsync(message, TestContext.Current.CancellationToken);
        Assert.Equal(2, calls);
        Assert.Null(message.PublishedAtUtc); // Only the dispatcher can commit delivery state.
        channel.Verify(x => x.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, true, false, null, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Does_not_return_success_before_confirm_or_swallow_publish_failure()
    {
        var channel = new Mock<IChannel>();
        var confirmation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = Message();
        channel.Setup(x => x.BasicPublishAsync(Exchange, message.EventType, true, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(confirmation.Task));
        await using var publisher = await RabbitOutboxPublisher.CreateAsync(Connection(channel).Object, Exchange, TestContext.Current.CancellationToken);
        var pending = publisher.PublishAsync(message, TestContext.Current.CancellationToken);
        Assert.False(pending.IsCompleted);
        confirmation.SetException(new IOException("simulated nack/return/connection failure"));
        await Assert.ThrowsAsync<IOException>(() => pending);
        Assert.Null(message.PublishedAtUtc);
    }

    [Fact]
    public async Task Initialization_failure_disposes_channel()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(x => x.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, true, false, null, false, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("declaration failed"));
        await Assert.ThrowsAsync<IOException>(() => RabbitOutboxPublisher.CreateAsync(Connection(channel).Object, Exchange, TestContext.Current.CancellationToken));
        channel.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("amq.reserved")]
    public async Task Invalid_exchange_does_not_open_channel(string exchange)
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        await Assert.ThrowsAsync<ArgumentException>(() => RabbitOutboxPublisher.CreateAsync(connection.Object, exchange, TestContext.Current.CancellationToken));
        connection.VerifyNoOtherCalls();
    }

    private static Mock<IConnection> Connection(Mock<IChannel> channel)
    {
        var connection = new Mock<IConnection>(MockBehavior.Strict);
        connection.Setup(x => x.CreateChannelAsync(It.Is<CreateChannelOptions>(o => o.PublisherConfirmationsEnabled && o.PublisherConfirmationTrackingEnabled), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);
        return connection;
    }
}
