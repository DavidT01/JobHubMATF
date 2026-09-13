using Catalog.DTOs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;

namespace Catalog.Clients;

public class ProfileApiClient : IProfileApiClient
{
    private readonly CandidateProfileGrpcService.CandidateProfileGrpcServiceClient _client;

    public ProfileApiClient(CandidateProfileGrpcService.CandidateProfileGrpcServiceClient client)
    {
        _client = client;
    }

    public async Task<CandidateProfileDto?> GetCandidateByIdAsync(string candidateId)
    {
        try
        {
            var identity = await _client.GetCandidateProfileByUserIdAsync(
                new GetProfileByUserIdRequest { UserId = candidateId });

            var profile = await _client.GetCandidateProfileAsync(
                new GetCandidateProfileRequest { ProfileId = identity.ProfileId });

            return new CandidateProfileDto
            {
                Id = profile.ProfileId,
                Location = profile.Location,
                Skills = profile.Skills.ToList(),
                Experience = profile.Experience.Select(e => new ExperienceDto
                {
                    StartDate = e.StartDate.ToDateTime(),
                    EndDate = e.EndDate?.ToDateTime()
                }).ToList()
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
