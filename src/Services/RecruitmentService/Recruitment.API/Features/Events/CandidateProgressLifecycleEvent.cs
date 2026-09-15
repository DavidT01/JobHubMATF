using Recruitment.API.Entities;

namespace Recruitment.API.Features.Events;

public sealed record CandidateProgressLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid CandidateProgressId,
    Guid CandidateProfileId,
    Guid RecruitmentProcessId,
    string JobId,
    string Status,
    Guid? CurrentSelectionRoundId,
    DateTimeOffset OccurredAtUtc)
{
    public const string RoundAdvancedType = "candidate.round-advanced.v1";
    public const string RejectedType = "candidate.rejected.v1";
    public const string HiredType = "candidate.hired.v1";

    public static CandidateProgressLifecycleEvent RoundAdvanced(CandidateProgress progress, string jobId) =>
        Create(progress, jobId, RoundAdvancedType);
    public static CandidateProgressLifecycleEvent Rejected(CandidateProgress progress, string jobId) =>
        Create(progress, jobId, RejectedType);
    public static CandidateProgressLifecycleEvent Hired(CandidateProgress progress, string jobId) =>
        Create(progress, jobId, HiredType);

    private static CandidateProgressLifecycleEvent Create(CandidateProgress progress, string jobId, string eventType) => new(
        Guid.NewGuid(), eventType, 1, progress.Id, progress.CandidateProfileId, progress.RecruitmentProcessId,
        jobId, progress.Status.ToString(), progress.CurrentSelectionRoundId, DateTimeOffset.UtcNow);
}
