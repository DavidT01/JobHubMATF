using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidateApplicationProgress;

public sealed record GetCandidateApplicationProgressQuery(Guid ApplicationId) : IRequest<CandidateApplicationProgressDto>;
