using MediatR;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;

namespace Profile.API.Features.CandidateProfiles.Commands.UploadPicture
{
    public class UploadCandidatePictureCommandHandler(IProfileContext context, IWebHostEnvironment environment, ILogger<UploadCandidatePictureCommandHandler> logger) : IRequestHandler<UploadCandidatePictureCommand, string?>
    {
        public async Task<string?> Handle(UploadCandidatePictureCommand request, CancellationToken cancellationToken)
        {
            var profile = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (profile == null)
            {
                logger.LogWarning("Candidate profile {Id} not found.", request.Id);
                return null;
            }

            var rootPath = environment.WebRootPath ?? throw new InvalidOperationException("WebRootPath is not configured.");
            var picturesFolder = Path.Combine(rootPath, "uploads", "pictures");
            Directory.CreateDirectory(picturesFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(picturesFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(fileStream, cancellationToken);
            }

            if (!string.IsNullOrEmpty(profile.PictureUrl))
            {
                var oldFilePath = Path.Combine(picturesFolder, Path.GetFileName(profile.PictureUrl));

                if (File.Exists(oldFilePath))
                    File.Delete(oldFilePath);
            }

            var fileUrl = $"/uploads/pictures/{fileName}";
            profile.PictureUrl = fileUrl;
            profile.ModifiedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Saved picture for candidate {Id} at path {Path}", profile.Id, fileUrl);

            return fileUrl;
        }
    }
}