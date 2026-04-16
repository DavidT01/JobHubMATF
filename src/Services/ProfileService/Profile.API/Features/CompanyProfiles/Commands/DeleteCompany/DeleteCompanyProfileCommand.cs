using MediatR;

namespace Profile.API.Features.CompanyProfiles.Commands.DeleteCompany
{
    public record DeleteCompanyProfileCommand(Guid Id) : IRequest<bool>;
}
