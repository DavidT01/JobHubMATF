using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile
{
    public class GetCandidateProfileQueryHandler(IProfileContext context, IMapper mapper, ILogger<GetCandidateProfileQueryHandler> logger) : IRequestHandler<GetCandidateProfileQuery, CandidateProfileDto?>
    {
        public async Task<CandidateProfileDto?> Handle(GetCandidateProfileQuery request, CancellationToken cancellationToken)
        {
            var profile = await context.CandidateProfiles
                .Include(p => p.Education)
                .Include(p => p.Experience)
                .Include(p => p.Projects)
                .Include(p => p.Languages)
                .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

            if(profile == null)
            {
                logger.LogWarning("Candidate profile {UserId} not found.", request.UserId);
                return null;
            }

            return mapper.Map<CandidateProfileDto>(profile);
        }
    }
}
