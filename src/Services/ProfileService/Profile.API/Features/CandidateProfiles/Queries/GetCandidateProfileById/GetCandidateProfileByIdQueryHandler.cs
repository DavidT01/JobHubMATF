using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfileById;

public class GetCandidateProfileByIdQueryHandler(
    IProfileContext context,
    IMapper mapper,
    ILogger<GetCandidateProfileByIdQueryHandler> logger)
    : IRequestHandler<GetCandidateProfileByIdQuery, CandidateProfileDto?>
{
    public async Task<CandidateProfileDto?> Handle(
        GetCandidateProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await context.CandidateProfiles
            .Include(p => p.Education)
            .Include(p => p.Experience)
            .Include(p => p.Projects)
            .Include(p => p.Languages)
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("Candidate profile {ProfileId} not found.", request.ProfileId);
            return null;
        }

        return mapper.Map<CandidateProfileDto>(profile);
    }
}
