namespace Profile.API.Infrastructure;

public interface IProfileAuthorization
{
    string GetCurrentUserId();

    Task EnsureUserAsync(string userId, CancellationToken cancellationToken);

    Task EnsureCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task EnsureCompanyProfileAsync(Guid profileId, CancellationToken cancellationToken);
}
