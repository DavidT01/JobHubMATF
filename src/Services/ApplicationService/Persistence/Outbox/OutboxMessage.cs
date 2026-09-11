using System.Text.Json;
using ApplicationService.Application.Events;

namespace ApplicationService.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public Guid Id { get; private set; }
    public long Sequence { get; private set; }
    public Guid ApplicationId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    public static OutboxMessage Create(ApplicationLifecycleEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (message.EventId == Guid.Empty || message.ApplicationId == Guid.Empty || message.SchemaVersion != 1
            || message.EventType is not (ApplicationLifecycleEvent.SubmittedType or ApplicationLifecycleEvent.StatusChangedType))
            throw new ArgumentException("A valid version-one application event is required.", nameof(message));
        return new OutboxMessage
        {
            Id = message.EventId,
            ApplicationId = message.ApplicationId,
            EventType = message.EventType,
            Payload = JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
            OccurredAtUtc = message.OccurredAtUtc.ToUniversalTime(),
            NextAttemptAtUtc = message.OccurredAtUtc.ToUniversalTime()
        };
    }

    public void RecordFailure(DateTimeOffset retryAtUtc)
    {
        if (PublishedAtUtc.HasValue) throw new InvalidOperationException("A published event cannot be retried.");
        if (retryAtUtc < NextAttemptAtUtc) throw new ArgumentOutOfRangeException(nameof(retryAtUtc));
        Attempts = checked(Attempts + 1);
        NextAttemptAtUtc = retryAtUtc.ToUniversalTime();
    }

    public void MarkPublished(DateTimeOffset publishedAtUtc)
    {
        if (PublishedAtUtc.HasValue) return;
        if (publishedAtUtc < OccurredAtUtc) throw new ArgumentOutOfRangeException(nameof(publishedAtUtc));
        PublishedAtUtc = publishedAtUtc.ToUniversalTime();
    }
}
