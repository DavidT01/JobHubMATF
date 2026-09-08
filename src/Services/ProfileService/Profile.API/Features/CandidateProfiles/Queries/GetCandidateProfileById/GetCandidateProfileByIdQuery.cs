using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfileById;

public record GetCandidateProfileByIdQuery(Guid ProfileId) : IRequest<CandidateProfileDto?>;
