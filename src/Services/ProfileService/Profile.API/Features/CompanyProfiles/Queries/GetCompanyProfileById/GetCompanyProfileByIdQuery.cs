using MediatR;
using Profile.API.DTOs;

namespace Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfileById;

public record GetCompanyProfileByIdQuery(Guid ProfileId) : IRequest<CompanyProfileDto?>;
