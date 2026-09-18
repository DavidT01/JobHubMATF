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

            return MapToDto(profile);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<CandidateProfileDto>> SearchCandidatesAsync(List<string> skills, string? location, int limit)
    {
        var request = new SearchCandidateRequest { Limit = limit };
        request.Skills.AddRange(skills);
        if (!string.IsNullOrWhiteSpace(location))
        {
            request.Location = location;
        }

        var response = await _client.SearchCandidatesAsync(request);
        return response.Candidates.Select(MapToDto).ToList();
    }

    public async Task<string?> GetCompanyProfileIdByUserIdAsync(string userId)
    {
        var identity = await _client.GetCompanyProfileByUserIdAsync(
            new GetProfileByUserIdRequest { UserId = userId });

        return string.IsNullOrEmpty(identity.ProfileId) ? null : identity.ProfileId;
    }

    private static CandidateProfileDto MapToDto(CandidateProfileResponse profile)
    {
        return new CandidateProfileDto
        {
            Id = profile.ProfileId,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Location = profile.Location,
            Skills = profile.Skills.ToList(),
            Experience = profile.Experience.Select(e => new ExperienceDto
            {
                StartDate = e.StartDate.ToDateTime(),
                EndDate = e.EndDate?.ToDateTime()
            }).ToList()
        };
    }
}
