using MediatR;

namespace Profile.API.Features.CompanyProfiles.Commands.CreateCompany
{
    public class CreateCompanyProfileCommand : IRequest<Guid>
    {
        public string UserId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; } = string.Empty;
    }
}
