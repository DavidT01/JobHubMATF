using AutoMapper;
using MediatR;
using Profile.API.Data;
using Profile.API.Data.Outbox;
using Profile.API.Entities;
using Profile.API.Features.Events;

namespace Profile.API.Features.CandidateProfiles.Commands.CreateCandidate
{
    public class CreateCandidateProfileCommandHandler(IProfileContext context, IMapper mapper, ILogger<CreateCandidateProfileCommandHandler> logger) : IRequestHandler<CreateCandidateProfileCommand, Guid>
    {
        public async Task<Guid> Handle(CreateCandidateProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = mapper.Map<CandidateProfile>(request);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;

            context.CandidateProfiles.Add(entity);
            context.OutboxMessages.Add(OutboxMessage.Create(CandidateProfileLifecycleEvent.Created(entity)));
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created candidate profile with Id {ProfileId} for user {UserId}", entity.Id, request.UserId);

            return entity.Id;
        }
    }
}
