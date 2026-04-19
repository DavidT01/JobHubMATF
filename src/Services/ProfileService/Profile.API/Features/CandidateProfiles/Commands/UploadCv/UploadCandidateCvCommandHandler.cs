using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CandidateProfiles.Commands.UploadCv
{
    public class UploadCandidateCvCommandHandler(IProfileContext context, IWebHostEnvironment environment, ILogger<UploadCandidateCvCommandHandler> logger) : IRequestHandler<UploadCandidateCvCommand, string?>
    {
        public async Task<string?> Handle(UploadCandidateCvCommand request, CancellationToken cancellationToken)
        {
            var profile = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (profile == null)
            {
                logger.LogWarning("Candidate profile {Id} not found.", request.Id);
                return null;
            }

            var rootPath = environment.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured.");
            var cvsFolder = Path.Combine(rootPath, "uploads", "cvs");
            Directory.CreateDirectory(cvsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(cvsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream, cancellationToken);
            }

            var fileUrl = $"/uploads/cvs/{fileName}";
            profile.CvUrl = fileUrl;
            profile.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved CV for candidate {Id} at path {Path}", profile.Id, fileUrl);

            return fileUrl;
        }
    }
}
