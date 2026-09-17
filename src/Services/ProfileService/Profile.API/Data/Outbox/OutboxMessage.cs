using System.Text.Json;
using Profile.API.Features.Events;

namespace Profile.API.Data.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    public Guid Id { get; private set; }
    public long Sequence { get; private set; }
    public Guid AggregateId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    public static OutboxMessage Create(CandidateProfileLifecycleEvent message) =>
        Create(message.EventId, message.CandidateProfileId, message.EventType, message, message.OccurredAtUtc);

    public static OutboxMessage Create(CompanyProfileLifecycleEvent message) =>
        Create(message.EventId, message.CompanyProfileId, message.EventType, message, message.OccurredAtUtc);

    private static OutboxMessage Create(Guid eventId, Guid aggregateId, string eventType, object payload, DateTimeOffset occurredAtUtc)
    {
        if (eventId == Guid.Empty || aggregateId == Guid.Empty || string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("A valid event id, aggregate id and event type are required.");
        return new OutboxMessage
        {
            Id = eventId,
            AggregateId = aggregateId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            OccurredAtUtc = occurredAtUtc.ToUniversalTime(),
            NextAttemptAtUtc = occurredAtUtc.ToUniversalTime()
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
