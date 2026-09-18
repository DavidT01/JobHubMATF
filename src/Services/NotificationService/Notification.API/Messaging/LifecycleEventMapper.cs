using System.Text.Json;

namespace Notification.API.Messaging;

public static class LifecycleEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LifecycleNotification? TryMap(string routingKey, ReadOnlySpan<byte> body)
    {
        if (string.IsNullOrWhiteSpace(routingKey) || body.IsEmpty)
        {
            return null;
        }

        return routingKey switch
        {
            "application.submitted.v1" or "application.status-changed.v1"
                => MapApplication(routingKey, body),
            "interview.scheduled.v1" or "interview.rescheduled.v1" or "interview.cancelled.v1"
                => MapInterview(routingKey, body),
            "candidate.round-advanced.v1" or "candidate.rejected.v1" or "candidate.hired.v1"
                => MapCandidateProgress(routingKey, body),
            _ => null
        };
    }

    private static LifecycleNotification? MapApplication(string eventType, ReadOnlySpan<byte> body)
    {
        var payload = JsonSerializer.Deserialize<ApplicationLifecyclePayload>(body, JsonOptions);
        if (payload is null || payload.EventId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.CandidateUserId))
        {
            return null;
        }

        var job = ShortJob(payload.JobId);
        if (eventType == "application.submitted.v1")
        {
            return new LifecycleNotification(
                payload.EventId,
                eventType,
                payload.CandidateUserId,
                "Application submitted",
                $"Your application for job {job} was submitted.");
        }

        var status = string.IsNullOrWhiteSpace(payload.Status) ? "updated" : payload.Status;
        return new LifecycleNotification(
            payload.EventId,
            eventType,
            payload.CandidateUserId,
            "Application status updated",
            $"Your application for job {job} is now {status}.");
    }

    private static LifecycleNotification? MapInterview(string eventType, ReadOnlySpan<byte> body)
    {
        var payload = JsonSerializer.Deserialize<InterviewLifecyclePayload>(body, JsonOptions);
        if (payload is null || payload.EventId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.CandidateUserId))
        {
            return null;
        }

        var when = payload.StartTimeUtc.ToString("u");
        return eventType switch
        {
            "interview.scheduled.v1" => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "Interview scheduled",
                $"An interview was scheduled for {when}."),
            "interview.rescheduled.v1" => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "Interview rescheduled",
                $"Your interview was moved to {when}."),
            _ => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "Interview cancelled",
                "Your interview was cancelled.")
        };
    }

    private static LifecycleNotification? MapCandidateProgress(string eventType, ReadOnlySpan<byte> body)
    {
        var payload = JsonSerializer.Deserialize<CandidateProgressPayload>(body, JsonOptions);
        if (payload is null || payload.EventId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.CandidateUserId))
        {
            return null;
        }

        var job = ShortJob(payload.JobId);
        return eventType switch
        {
            "candidate.round-advanced.v1" => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "Moved to next round",
                $"You advanced to the next selection round for job {job}."),
            "candidate.hired.v1" => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "You were hired",
                $"Congratulations — you were hired for job {job}."),
            _ => new LifecycleNotification(
                payload.EventId, eventType, payload.CandidateUserId,
                "Application closed",
                $"Your candidacy for job {job} was rejected.")
        };
    }

    private static string ShortJob(string? jobId) =>
        string.IsNullOrWhiteSpace(jobId) ? "unknown" : (jobId.Length <= 12 ? jobId : jobId[..12] + "…");

    private sealed record ApplicationLifecyclePayload(
        Guid EventId,
        string? CandidateUserId,
        string? JobId,
        string? Status);

    private sealed record InterviewLifecyclePayload(
        Guid EventId,
        string? CandidateUserId,
        DateTimeOffset StartTimeUtc);

    private sealed record CandidateProgressPayload(
        Guid EventId,
        string? CandidateUserId,
        string? JobId);
}
