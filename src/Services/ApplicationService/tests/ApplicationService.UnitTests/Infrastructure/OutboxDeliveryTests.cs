using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class OutboxDeliveryTests
{
    private static OutboxMessage Message() => OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(
        JobApplication.Create(Guid.NewGuid(), "user", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow.AddMinutes(-1))));

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(5, 160)]
    [InlineData(6, 300)]
    [InlineData(int.MaxValue, 300)]
    public void Retry_is_exponential_and_capped(int attempts, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), OutboxDelivery.RetryDelay(attempts));

    [Fact]
    public async Task Failure_preserves_payload_and_schedules_retry()
    {
        var row = Message();
        var originalPayload = row.Payload;
        var publisher = new Mock<IOutboxPublisher>();
        publisher.Setup(x => x.PublishAsync(row, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("failure"));
        var delivery = Delivery(publisher.Object);
        Assert.False(await delivery.DeliverAsync(row, TestContext.Current.CancellationToken));
        Assert.Equal(1, row.Attempts);
        Assert.Null(row.PublishedAtUtc);
        Assert.Equal(originalPayload, row.Payload);
        Assert.True(row.NextAttemptAtUtc > row.OccurredAtUtc);
    }

    [Fact]
    public async Task Successful_confirmation_marks_complete_once()
    {
        var row = Message();
        var publisher = new Mock<IOutboxPublisher>();
        publisher.Setup(x => x.PublishAsync(row, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var delivery = Delivery(publisher.Object);
        Assert.True(await delivery.DeliverAsync(row, TestContext.Current.CancellationToken));
        Assert.NotNull(row.PublishedAtUtc);
        Assert.True(await delivery.DeliverAsync(row, TestContext.Current.CancellationToken));
        publisher.Verify(x => x.PublishAsync(row, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Shutdown_cancellation_does_not_mark_failure_or_completion()
    {
        var row = Message();
        using var cancellation = new CancellationTokenSource();
        var publisher = new Mock<IOutboxPublisher>();
        publisher.Setup(x => x.PublishAsync(row, It.IsAny<CancellationToken>())).Returns(() =>
        {
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Delivery(publisher.Object).DeliverAsync(row, cancellation.Token));
        Assert.Equal(0, row.Attempts);
        Assert.Null(row.PublishedAtUtc);
    }

    private static OutboxDelivery Delivery(IOutboxPublisher publisher) => new(publisher,
        TimeProvider.System, NullLogger<OutboxDelivery>.Instance);
}
