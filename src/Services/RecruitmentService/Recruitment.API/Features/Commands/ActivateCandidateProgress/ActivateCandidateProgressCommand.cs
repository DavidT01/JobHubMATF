using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.ActivateCandidateProgress;

public sealed record ActivateCandidateProgressCommand(
    Guid CandidateProfileId,
    string JobId,
    Guid? ApplicationId = null) : IRequest<CandidateProgressDto>;
