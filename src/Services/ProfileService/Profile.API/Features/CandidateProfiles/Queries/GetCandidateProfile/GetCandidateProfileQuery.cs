using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile
{
    public record GetCandidateProfileQuery(string UserId) : IRequest<CandidateProfileDto?>;
}
