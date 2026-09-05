using MediatR;
using Recruitment.API.DTOs;
using Recruitment.API.Enums;

namespace Recruitment.API.Features.Commands.UpdateCandidateStatus;

public class UpdateCandidateStatusCommand : IRequest<CandidateProgressDto>
{
    public Guid CandidateProfileId { get; init; }
    public Guid RecruitmentProcessId { get; init; }
    public CandidateProgressStatus Status { get; init; }
}
