using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CompanyProfiles.Commands.UpdateCompany
{
    public class UpdateCompanyProfileCommandHandler(IProfileContext context, IMapper mapper, ILogger<UpdateCompanyProfileCommandHandler> logger) : IRequestHandler<UpdateCompanyProfileCommand, bool>
    {
        public async Task<bool> Handle(UpdateCompanyProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await context.CompanyProfiles.FirstOrDefaultAsync(p => p.Id == request.Id && p.UserId == request.UserId, cancellationToken);

            if (entity == null)
            {
                logger.LogWarning("Company profile {ProfileId} not found.", request.Id);
                return false;
            }

            mapper.Map(request, entity);

            entity.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully updated company profile {ProfileId}", request.Id);

            return true;
        }
    }
}
