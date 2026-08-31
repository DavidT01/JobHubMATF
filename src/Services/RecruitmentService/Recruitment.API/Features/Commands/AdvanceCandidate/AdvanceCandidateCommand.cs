using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.AdvanceCandidate
{
    public class AdvanceCandidateCommand : IRequest<CandidateProgressDto>
    {
        public Guid CandidateProfileId { get; set; }
        public Guid RecruitmentProcessId { get; set; }
    }
}
