using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidatesInRound;

public record GetCandidatesInRoundQuery(Guid SelectionRoundId) : IRequest<List<CandidateProgressDto>>;
