using ApplicationService.Application.DTOs;

namespace ApplicationService.Application.Queries;

public sealed record GetCandidateApplicationDetailsQuery(Guid ApplicationId) : IQuery<CandidateApplicationDetailsDto>;
