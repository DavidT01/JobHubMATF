using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetCandidatesInRound;

public class GetCandidatesInRoundQueryHandler(RecruitmentContext context, IMapper mapper, ILogger<GetCandidatesInRoundQueryHandler> logger)
    : IRequestHandler<GetCandidatesInRoundQuery, List<CandidateProgressDto>>
{
    public async Task<List<CandidateProgressDto>> Handle(GetCandidatesInRoundQuery request, CancellationToken cancellationToken)
    {
        var candidates = await context.Progresses
            .Where(progress => progress.CurrentSelectionRoundId == request.SelectionRoundId)
            .OrderBy(progress => progress.CreatedAt)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {CandidateCount} candidates for selection round {SelectionRoundId}", candidates.Count, request.SelectionRoundId);
        return mapper.Map<List<CandidateProgressDto>>(candidates);
    }
}
