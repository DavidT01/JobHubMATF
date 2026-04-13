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
            var entity = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if(entity == null)
            {
                logger.LogWarning("Candidate profile {ProfileId} not found.", request.Id);
                return false;
            }

            mapper.Map(request, entity);

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully updated candidate profile {ProfileId}", request.Id);

            return true;
        }
    }
}
