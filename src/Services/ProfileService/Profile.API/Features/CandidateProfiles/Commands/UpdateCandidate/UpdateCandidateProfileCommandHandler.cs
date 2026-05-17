using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate
{
    public class UpdateCandidateProfileCommandHandler(IProfileContext context, IMapper mapper, ILogger<UpdateCandidateProfileCommandHandler> logger) : IRequestHandler<UpdateCandidateProfileCommand, bool>
    {
        public async Task<bool> Handle(UpdateCandidateProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await context.CandidateProfiles
                .Include(p => p.Education)
                .Include(p => p.Experience)
                .Include(p => p.Projects)
                .Include(p => p.Languages)
                .FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == request.UserId, cancellationToken);

            if(entity == null)
            {
                logger.LogWarning("Candidate profile {ProfileId} not found.", request.Id);
                return false;
            }

            mapper.Map(request, entity);

            entity.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully updated candidate profile {ProfileId}", request.Id);

            return true;
        }
    }
}
