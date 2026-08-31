using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidateEvaluations
{
    public class GetCandidateEvaluationsQuery(Guid candidateId) : IRequest<List<CandidateEvaluationDto>>
    {
        public Guid CandidateProfileId { get; set; } = candidateId;
    }
}
