using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile
{
    public record GetCompanyProfileQuery(string UserId) : IRequest<CompanyProfileDto?>;
}
