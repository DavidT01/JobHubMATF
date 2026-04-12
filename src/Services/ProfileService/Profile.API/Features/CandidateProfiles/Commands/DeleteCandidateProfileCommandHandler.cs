using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CandidateProfiles.Commands
{
    public class DeleteCandidateProfileCommandHandler(IProfileContext context, ILogger<DeleteCandidateProfileCommandHandler> logger) : IRequestHandler<DeleteCandidateProfileCommand, bool>
    {
        public async Task<bool> Handle(DeleteCandidateProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if(entity == null)
            {
                logger.LogWarning("Candidate profile {ProfileId} not found.", request.Id);
                return false;
            }

            context.CandidateProfiles.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully deleted candidate profile {ProfileId}.", request.Id);

            return true;
        }
    }
}
