using Profile.API.Entities;

namespace Profile.API.Features.Events;

public sealed record CompanyProfileLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid CompanyProfileId,
    string UserId,
    string CompanyName,
    DateTimeOffset OccurredAtUtc)
{
    public const string CreatedType = "company-profile.created.v1";
    public const string UpdatedType = "company-profile.updated.v1";

    public static CompanyProfileLifecycleEvent Created(CompanyProfile profile) => Create(profile, CreatedType);
    public static CompanyProfileLifecycleEvent Updated(CompanyProfile profile) => Create(profile, UpdatedType);

    private static CompanyProfileLifecycleEvent Create(CompanyProfile profile, string eventType) => new(
        Guid.NewGuid(), eventType, 1, profile.Id, profile.UserId, profile.CompanyName, DateTimeOffset.UtcNow);
}
