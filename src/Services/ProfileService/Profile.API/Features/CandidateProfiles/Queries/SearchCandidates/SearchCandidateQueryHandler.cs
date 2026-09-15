using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.SearchCandidates;

public class SearchCandidateQueryHandler(IProfileContext context , IMapper mapper)
    : IRequestHandler<SearchCandidateQuery,List<CandidateProfileDto>>
{
    public async Task<List<CandidateProfileDto>> Handle(SearchCandidateQuery request, CancellationToken cancellationToken)
    {
        var query = context.CandidateProfiles
            .Include(p => p.Education)
            .Include(p => p.Experience)
            .Include(p => p.Projects)
            .Include(p => p.Languages)
            .AsQueryable();

        if (request.Skills.Count > 0)
        {
            query = query.Where(p => p.Skills.Any(s => request.Skills.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            query = query.Where(p => p.Location == request.Location);
        }

        var profiles = await query.Take(request.Limit > 0 ? request.Limit : 20).ToListAsync(cancellationToken);
        return mapper.Map<List<CandidateProfileDto>>(profiles);
    }
}