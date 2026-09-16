using Profile.API.Entities;

namespace Profile.API.Features.Events;

public sealed record CandidateProfileLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid CandidateProfileId,
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? CvUrl,
    DateTimeOffset OccurredAtUtc)
{
    public const string CreatedType = "candidate-profile.created.v1";
    public const string UpdatedType = "candidate-profile.updated.v1";
    public const string CvUploadedType = "candidate-profile.cv-uploaded.v1";

    public static CandidateProfileLifecycleEvent Created(CandidateProfile profile) => Create(profile, CreatedType);
    public static CandidateProfileLifecycleEvent Updated(CandidateProfile profile) => Create(profile, UpdatedType);
    public static CandidateProfileLifecycleEvent CvUploaded(CandidateProfile profile) => Create(profile, CvUploadedType);

    private static CandidateProfileLifecycleEvent Create(CandidateProfile profile, string eventType) => new(
        Guid.NewGuid(), eventType, 1, profile.Id, profile.UserId, profile.FirstName,
        profile.LastName, profile.Email, profile.CvUrl, DateTimeOffset.UtcNow);
}
