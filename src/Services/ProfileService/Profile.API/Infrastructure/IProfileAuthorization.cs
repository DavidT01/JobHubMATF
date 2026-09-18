namespace Profile.API.Infrastructure;

public interface IProfileAuthorization
{
    string GetCurrentUserId();

    Task EnsureCanReadCandidateProfileAsync(string userId, CancellationToken cancellationToken);

    Task EnsureCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task EnsureCompanyProfileAsync(Guid profileId, CancellationToken cancellationToken);
}
