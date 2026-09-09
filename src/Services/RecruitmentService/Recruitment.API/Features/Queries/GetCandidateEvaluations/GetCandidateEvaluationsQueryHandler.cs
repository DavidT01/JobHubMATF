using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidateEvaluations
{
    public class GetCandidateEvaluationsQueryHandler(RecruitmentContext context, IMapper mapper, ILogger<GetCandidateEvaluationsQueryHandler> logger) : IRequestHandler<GetCandidateEvaluationsQuery, List<CandidateEvaluationDto>>
    {
        private readonly RecruitmentContext _context = context;
        private readonly IMapper _mapper = mapper;

        public async Task<List<CandidateEvaluationDto>> Handle(GetCandidateEvaluationsQuery request, CancellationToken cancellationToken)
        {
            var evaluations = await _context.Evaluations
                .Where(e => e.CandidateProfileId == request.CandidateProfileId)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Retrieved {EvaluationCount} evaluations for candidate {CandidateProfileId}", evaluations.Count, request.CandidateProfileId);
            return _mapper.Map<List<CandidateEvaluationDto>>(evaluations);
        }
    }
}
