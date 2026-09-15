using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.SearchCandidates
{
    public record SearchCandidateQuery(List<string> Skills, string? Location, int Limit)
        : IRequest<List<CandidateProfileDto>>;
}