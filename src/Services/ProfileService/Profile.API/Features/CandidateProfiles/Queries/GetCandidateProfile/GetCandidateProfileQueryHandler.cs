using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTO;

namespace Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile
{
    public class GetCandidateProfileQueryHandler(IProfileContext context, IMapper mapper, ILogger<GetCandidateProfileQueryHandler> logger) : IRequestHandler<GetCandidateProfileQuery, CandidateProfileDto?>
    {
public async Task<CandidateProfileDto?> Handle(GetCandidateProfileQuery request, CancellationToken cancellationToken)
        {
            var profile = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

            if(profile == null)
            {
                logger.LogWarning("Candidate profile {UserId} not found.", request.UserId);
                return null;
            }

            return mapper.Map<CandidateProfileDto>(profile);
        }
    }
}
