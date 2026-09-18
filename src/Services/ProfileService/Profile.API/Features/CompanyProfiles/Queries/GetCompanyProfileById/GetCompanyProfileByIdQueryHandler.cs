using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.DTOs;

namespace Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfileById;

public class GetCompanyProfileByIdQueryHandler(
    IProfileContext context,
    IMapper mapper,
    ILogger<GetCompanyProfileByIdQueryHandler> logger)
    : IRequestHandler<GetCompanyProfileByIdQuery, CompanyProfileDto?>
{
    public async Task<CompanyProfileDto?> Handle(
        GetCompanyProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await context.CompanyProfiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile is null)
        {
            logger.LogWarning("Company profile {ProfileId} not found.", request.ProfileId);
            return null;
        }

        return mapper.Map<CompanyProfileDto>(profile);
    }
}
