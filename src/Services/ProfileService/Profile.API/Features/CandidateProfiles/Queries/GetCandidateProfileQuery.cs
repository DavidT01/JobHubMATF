using MediatR;
using Profile.API.DTO;

namespace Profile.API.Features.CandidateProfiles.Queries
{
    public record GetCandidateProfileQuery(string UserId) : IRequest<CandidateProfileDto?>;
}
