using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.EvaluateCandidate
{
    public class EvaluateCandidateCommand : IRequest<CandidateEvaluationDto>
    {
        public Guid CandidateProfileId { get; set; }
        public Guid SelectionRoundId { get; set; }
        public int Score { get; set; }
        public string? Notes { get; set; }
    }
}
