using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class OutboxMessagingTests
{
    [Fact]
    public void Disabled_configuration_registers_no_worker_or_broker_connection()
    {
        var services = new ServiceCollection();
        services.AddOutboxMessaging(new ConfigurationBuilder().Build());
        Assert.Empty(services);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://localhost")]
    [InlineData("amqp://user:secret@rabbitmq")]
    [InlineData("amqps://rabbitmq")]
    [InlineData("amqps://user:secret@rabbitmq/?query=value")]
    public void Unsafe_or_incomplete_uri_is_rejected_without_leaking_credentials(string? uri)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new OutboxMessagingOptions { ConnectionUri = uri }.CreateConnectionFactory());
        Assert.DoesNotContain("secret", error.Message);
        Assert.Null(error.InnerException);
    }

    [Theory]
    [InlineData("amqp://user:secret@localhost:5672")]
    [InlineData("amqps://user:secret@rabbitmq:5671")]
    public void Valid_configuration_creates_factory_without_connecting(string uri)
    {
        var factory = Assert.IsType<ConnectionFactory>(new OutboxMessagingOptions { ConnectionUri = uri }.CreateConnectionFactory());
        Assert.False(factory.AutomaticRecoveryEnabled);
        Assert.Equal(TimeSpan.FromSeconds(10), factory.RequestedConnectionTimeout);
        Assert.Equal("jobhub-application-outbox", factory.ClientProvidedName);
    }

    [Fact]
    public async Task Connection_is_reused_and_disposed_after_publish_failure()
    {
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        var factory = new Mock<IConnectionFactory>();
        connection.SetupGet(x => x.IsOpen).Returns(true);
        connection.Setup(x => x.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(channel.Object);
        factory.Setup(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        var message = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(JobApplication.Create(Guid.NewGuid(), "candidate",
            "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow)));
        await using var session = new RabbitPublisherConnection(factory.Object, new OutboxMessagingOptions(), NullLogger<RabbitPublisherConnection>.Instance);
        await session.PublishAsync(message, TestContext.Current.CancellationToken);
        await session.PublishAsync(message, TestContext.Current.CancellationToken);
        factory.Verify(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        channel.Setup(x => x.BasicPublishAsync(It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException(new IOException("publish failed")));
        await Assert.ThrowsAsync<IOException>(() => session.PublishAsync(message, TestContext.Current.CancellationToken));
        connection.Verify(x => x.DisposeAsync(), Times.Once);
        channel.Verify(x => x.DisposeAsync(), Times.Once);
        await Assert.ThrowsAsync<IOException>(() => session.PublishAsync(message, TestContext.Current.CancellationToken));
        factory.Verify(x => x.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
