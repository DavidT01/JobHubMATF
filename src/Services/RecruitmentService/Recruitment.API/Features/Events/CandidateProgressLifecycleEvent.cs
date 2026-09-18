using Recruitment.API.Entities;

namespace Recruitment.API.Features.Events;

public sealed record CandidateProgressLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid CandidateProgressId,
    Guid CandidateProfileId,
    string? CandidateUserId,
    Guid RecruitmentProcessId,
    string JobId,
    string Status,
    Guid? CurrentSelectionRoundId,
    DateTimeOffset OccurredAtUtc)
{
    public const string RoundAdvancedType = "candidate.round-advanced.v1";
    public const string RejectedType = "candidate.rejected.v1";
    public const string HiredType = "candidate.hired.v1";

    public static CandidateProgressLifecycleEvent RoundAdvanced(
        CandidateProgress progress, string jobId, string? candidateUserId) =>
        Create(progress, jobId, RoundAdvancedType, candidateUserId);
    public static CandidateProgressLifecycleEvent Rejected(
        CandidateProgress progress, string jobId, string? candidateUserId) =>
        Create(progress, jobId, RejectedType, candidateUserId);
    public static CandidateProgressLifecycleEvent Hired(
        CandidateProgress progress, string jobId, string? candidateUserId) =>
        Create(progress, jobId, HiredType, candidateUserId);

    private static CandidateProgressLifecycleEvent Create(
        CandidateProgress progress, string jobId, string eventType, string? candidateUserId) => new(
        Guid.NewGuid(), eventType, 1, progress.Id, progress.CandidateProfileId, candidateUserId,
        progress.RecruitmentProcessId, jobId, progress.Status.ToString(), progress.CurrentSelectionRoundId,
        DateTimeOffset.UtcNow);
}
