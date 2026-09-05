using JobHub.Grpc.Contracts.Profile;

namespace Recruitment.API.Infrastructure;

public interface IProfileServiceClient
{
    Task<CandidateContactResponse> GetCandidateContactAsync(Guid profileId, CancellationToken cancellationToken);

    Task<bool> ValidateCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<CandidateProfileResponse> GetCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken);
}
