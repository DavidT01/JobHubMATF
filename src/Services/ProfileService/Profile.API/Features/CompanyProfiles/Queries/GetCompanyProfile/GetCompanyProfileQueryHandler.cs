using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTOs;

namespace Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile
{
    public class GetCompanyProfileQueryHandler(IProfileContext context, IMapper mapper, ILogger<GetCompanyProfileQueryHandler> logger) : IRequestHandler<GetCompanyProfileQuery, CompanyProfileDto?>
    {
        public async Task<CompanyProfileDto?> Handle(GetCompanyProfileQuery request, CancellationToken cancellationToken)
        {
            var profile = await context.CompanyProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

            if (profile == null)
            {
                logger.LogWarning("Company profile {UserId} not found.", request.UserId);
                return null;
            }

            return mapper.Map<CompanyProfileDto>(profile);
        }
    }
}
