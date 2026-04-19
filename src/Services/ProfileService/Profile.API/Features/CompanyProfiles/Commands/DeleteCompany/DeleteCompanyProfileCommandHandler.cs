using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CompanyProfiles.Commands.DeleteCompany
{
    public class DeleteCompanyProfileCommandHandler(IProfileContext context, ILogger<DeleteCompanyProfileCommandHandler> logger) : IRequestHandler<DeleteCompanyProfileCommand, bool>
    {
        public async Task<bool> Handle(DeleteCompanyProfileCommand request, CancellationToken cancellationToken)
        {
            var entity = await context.CompanyProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (entity == null)
            {
                logger.LogWarning("Company profile {ProfileId} not found.", request.Id);
                return false;
            }

            context.CompanyProfiles.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully deleted company profile {ProfileId}.", request.Id);

            return true;
        }
    }
}
