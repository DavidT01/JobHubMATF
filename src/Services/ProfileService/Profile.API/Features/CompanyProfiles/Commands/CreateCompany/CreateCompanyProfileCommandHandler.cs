using AutoMapper;
using MediatR;
using Profile.API.Data;
using Profile.API.Data.Outbox;
using Profile.API.Entities;
using Profile.API.Features.Events;

namespace Profile.API.Features.CompanyProfiles.Commands.CreateCompany
{
    public class CreateCompanyProfileCommandHandler(IProfileContext context, IMapper mapper, ILogger<CreateCompanyProfileCommandHandler> logger) : IRequestHandler<CreateCompanyProfileCommand, Guid>
    {
        public async Task<Guid> Handle(CreateCompanyProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = mapper.Map<CompanyProfile>(request);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;

            context.CompanyProfiles.Add(entity);
            context.OutboxMessages.Add(OutboxMessage.Create(CompanyProfileLifecycleEvent.Created(entity)));
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created company profile with Id {ProfileId} for user {UserId}", entity.Id, request.UserId);

            return entity.Id;
        }
    }
}
