using ApplicationService.Persistence.Outbox;

namespace ApplicationService.Infrastructure.Messaging;

public sealed class OutboxDelivery(IOutboxPublisher publisher, TimeProvider clock, ILogger<OutboxDelivery> logger)
{
    public async Task<bool> DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (message.PublishedAtUtc.HasValue) return true;
        try
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var retryAt = clock.GetUtcNow().Add(RetryDelay(message.Attempts));
            message.RecordFailure(retryAt < message.NextAttemptAtUtc ? message.NextAttemptAtUtc : retryAt);
            // Broker exception messages can contain endpoints or credentials.
            logger.LogWarning("Outbox event {EventId} failed ({FailureType}); attempt {Attempt} scheduled for {RetryAt}.",
                message.Id, exception.GetType().Name, message.Attempts, message.NextAttemptAtUtc);
            return false;
        }
        message.MarkPublished(clock.GetUtcNow());
        return true;
    }

    public static TimeSpan RetryDelay(int previousFailures)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(previousFailures);
        return TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, Math.Min(previousFailures, 6))));
    }
}
