using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.Events;

// Versioned payload shared through the broker, not a serialized EF entity.
public sealed record ApplicationLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid ApplicationId,
    Guid CandidateProfileId,
    string? CandidateUserId,
    string JobId,
    string Status,
    string? PreviousStatus,
    DateTimeOffset OccurredAtUtc)
{
    public const string SubmittedType = "application.submitted.v1";
    public const string StatusChangedType = "application.status-changed.v1";

    public static ApplicationLifecycleEvent Submitted(JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (application.Status != ApplicationStatus.Submitted)
            throw new ArgumentException("A submitted event requires a newly submitted application.", nameof(application));
        return Create(application, SubmittedType, null, application.SubmittedAtUtc);
    }

    public static ApplicationLifecycleEvent StatusChanged(JobApplication application, ApplicationStatus previousStatus)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (!Enum.IsDefined(previousStatus) || previousStatus == application.Status)
            throw new ArgumentException("A status event requires a distinct valid previous status.", nameof(previousStatus));
        return Create(application, StatusChangedType, previousStatus.ToString(), application.UpdatedAtUtc);
    }

    private static ApplicationLifecycleEvent Create(JobApplication application, string type,
        string? previousStatus, DateTimeOffset occurredAt) => new(
            Guid.NewGuid(), type, 1, application.Id, application.CandidateId,
            application.CandidateUserId, application.JobId, application.Status.ToString(),
            previousStatus, occurredAt.ToUniversalTime());
}
