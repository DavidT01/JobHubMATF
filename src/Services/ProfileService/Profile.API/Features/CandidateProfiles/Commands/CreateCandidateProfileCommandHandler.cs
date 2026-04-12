using AutoMapper;
using MediatR;
using Profile.API.Data;
using Profile.API.Entities;

namespace Profile.API.Features.CandidateProfiles.Commands
{
    public class CreateCandidateProfileCommandHandler(IProfileContext context, IMapper mapper, ILogger<CreateCandidateProfileCommandHandler> logger) : IRequestHandler<CreateCandidateProfileCommand, Guid>
    {
        public async Task<Guid> Handle(CreateCandidateProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = mapper.Map<CandidateProfile>(request);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;

            context.CandidateProfiles.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully create candidate profile with Id {ProfileId} for user {UserId}", entity.Id, request.UserId);

            return entity.Id;
        }
    }
}
