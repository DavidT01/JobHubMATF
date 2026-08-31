using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidateProgress
{
    public class GetCandidateProgressQuery(Guid candidateId, Guid processId) : IRequest<CandidateProgressDto>
    {
        public Guid CandidateProfileId { get; set; } = candidateId;
        public Guid ProcessId { get; set; } = processId;
    }
}
