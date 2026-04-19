using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CompanyProfiles.Commands.UploadLogo
{
    public class UploadCompanyLogoCommandHandler(IProfileContext context, IWebHostEnvironment environment, ILogger<UploadCompanyLogoCommandHandler> logger) : IRequestHandler<UploadCompanyLogoCommand, string?>
    {
        public async Task<string?> Handle(UploadCompanyLogoCommand request, CancellationToken cancellationToken)
        {
            var profile = await context.CompanyProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (profile == null)
            {
                logger.LogWarning("Company profile {Id} not found.", request.Id);
                return null;
            }

            var rootPath = environment.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured.");
            var logosFolder = Path.Combine(rootPath, "uploads", "logos");
            Directory.CreateDirectory(logosFolder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.File.FileName)}";
            var filePath = Path.Combine(logosFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream, cancellationToken);
            }

            var fileUrl = $"/uploads/logos/{fileName}";
            profile.LogoUrl = fileUrl;
            profile.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved Logo for company {Id} at path {Path}", profile.Id, fileUrl);

            return fileUrl;
        }
    }
}
