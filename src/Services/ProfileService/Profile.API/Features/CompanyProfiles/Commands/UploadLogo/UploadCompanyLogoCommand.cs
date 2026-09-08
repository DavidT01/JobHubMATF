using MediatR;

namespace Profile.API.Features.CompanyProfiles.Commands.UploadLogo
{
    public class UploadCompanyLogoCommand : IRequest<string?>
    {
        public Guid Id { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}
