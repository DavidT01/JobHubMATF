using JobHub.Grpc.Contracts.Profile;

namespace Recruitment.API.Infrastructure;

public sealed class ProfileServiceClient(CandidateProfileGrpcService.CandidateProfileGrpcServiceClient client)
    : IProfileServiceClient
{
    public Task<CandidateContactResponse> GetCandidateContactAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return client.GetCandidateContactAsync(
            new GetCandidateContactRequest { ProfileId = profileId.ToString("D") },
            cancellationToken: cancellationToken).ResponseAsync;
    }

    public async Task<bool> ValidateCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var response = await client.ValidateCandidateProfileAsync(
            new ValidateCandidateProfileRequest { ProfileId = profileId.ToString("D") },
            cancellationToken: cancellationToken);

        return response.Exists;
    }

    public Task<CandidateProfileResponse> GetCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return client.GetCandidateProfileAsync(
            new GetCandidateProfileRequest { ProfileId = profileId.ToString("D") },
            cancellationToken: cancellationToken).ResponseAsync;
    }
}
