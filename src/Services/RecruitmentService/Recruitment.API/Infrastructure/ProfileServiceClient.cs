using JobHub.Grpc.Contracts.Profile;
using Grpc.Core;

namespace Recruitment.API.Infrastructure;

public sealed class ProfileServiceClient(
    CandidateProfileGrpcService.CandidateProfileGrpcServiceClient client,
    IHttpContextAccessor httpContextAccessor)
    : IProfileServiceClient
{
    public Task<CandidateContactResponse> GetCandidateContactAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return client.GetCandidateContactAsync(
            new GetCandidateContactRequest { ProfileId = profileId.ToString("D") },
            headers: GetHeaders(), cancellationToken: cancellationToken).ResponseAsync;
    }

    public async Task<bool> ValidateCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var response = await client.ValidateCandidateProfileAsync(
            new ValidateCandidateProfileRequest { ProfileId = profileId.ToString("D") },
            headers: GetHeaders(), cancellationToken: cancellationToken);

        return response.Exists;
    }

    public Task<CandidateProfileResponse> GetCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return client.GetCandidateProfileAsync(
            new GetCandidateProfileRequest { ProfileId = profileId.ToString("D") },
            headers: GetHeaders(), cancellationToken: cancellationToken).ResponseAsync;
    }

    public async Task<Guid?> GetCandidateProfileIdByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        var response = await client.GetCandidateProfileByUserIdAsync(
            new GetProfileByUserIdRequest { UserId = userId },
            headers: GetHeaders(), cancellationToken: cancellationToken);
        return ParseProfileId(response.ProfileId);
    }

    public async Task<Guid?> GetCompanyProfileIdByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        var response = await client.GetCompanyProfileByUserIdAsync(
            new GetProfileByUserIdRequest { UserId = userId },
            headers: GetHeaders(), cancellationToken: cancellationToken);
        return ParseProfileId(response.ProfileId);
    }

    private Metadata? GetHeaders()
    {
        var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        return string.IsNullOrWhiteSpace(authorization)
            ? null
            : new Metadata { { "authorization", authorization } };
    }

    private static Guid? ParseProfileId(string profileId) =>
        Guid.TryParse(profileId, out var parsed) ? parsed : null;
}
