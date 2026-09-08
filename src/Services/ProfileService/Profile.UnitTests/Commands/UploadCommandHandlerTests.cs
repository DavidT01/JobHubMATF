using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Profile.API.Entities;
using Profile.API.Features.CandidateProfiles.Commands.UploadCv;
using Profile.API.Features.CandidateProfiles.Commands.UploadPicture;
using Profile.API.Features.CompanyProfiles.Commands.UploadLogo;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Commands
{
    public class UploadHandlerTests
    {
        [Fact]
        public async Task UploadCandidateCv_ExistingProfile_SavesFileAndUrl()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidateCvCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidateCvCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCandidateCvCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("resume.pdf", "application/pdf", "cv-content")
            }, CancellationToken.None);

            result.Should().StartWith("/uploads/cvs/");
            profile.CvUrl.Should().Be(result);
            File.Exists(Path.Combine(root.Path, "uploads", "cvs", Path.GetFileName(result))).Should().BeTrue();
            profile.ModifiedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadCandidateCv_ExistingCv_DeletesPreviousFile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidateCvCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidateCvCommandHandler>.Instance);

            var firstResult = await handler.Handle(new UploadCandidateCvCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("resume.pdf", "application/pdf", "cv-content")
            }, CancellationToken.None);
            var previousFilePath = Path.Combine(root.Path, "uploads", "cvs", Path.GetFileName(firstResult!));

            var secondResult = await handler.Handle(new UploadCandidateCvCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("resume-v2.pdf", "application/pdf", "cv-content-v2")
            }, CancellationToken.None);

            secondResult.Should().NotBe(firstResult);
            File.Exists(previousFilePath).Should().BeFalse();
            File.Exists(Path.Combine(root.Path, "uploads", "cvs", Path.GetFileName(secondResult!))).Should().BeTrue();
        }

        [Fact]
        public async Task UploadCandidateCv_MissingProfile_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidateCvCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidateCvCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCandidateCvCommand
            {
                Id = Guid.NewGuid(),
                File = TestHelpers.CreateFile("resume.pdf", "application/pdf", "cv-content")
            }, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UploadCandidatePicture_MissingProfile_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidatePictureCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidatePictureCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCandidatePictureCommand
            {
                Id = Guid.NewGuid(),
                File = TestHelpers.CreateFile("picture.png", "image/png", "image-content")
            }, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UploadCandidatePicture_WithoutWebRoot_Throws()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            var handler = new UploadCandidatePictureCommandHandler(
                context,
                TestHelpers.CreateEnvironment(null),
                NullLogger<UploadCandidatePictureCommandHandler>.Instance);

            var action = () => handler.Handle(new UploadCandidatePictureCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("picture.png", "image/png", "image-content")
            }, CancellationToken.None);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task UploadCandidatePicture_ExistingProfile_SavesFileAndUrl()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidatePictureCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidatePictureCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCandidatePictureCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("picture.png", "image/png", "image-content")
            }, CancellationToken.None);

            result.Should().StartWith("/uploads/pictures/");
            File.Exists(Path.Combine(root.Path, "uploads", "pictures", Path.GetFileName(result))).Should().BeTrue();
        }

        [Fact]
        public async Task UploadCandidatePicture_ExistingPicture_DeletesPreviousFile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CandidateProfile { UserId = "candidate-1" };
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCandidatePictureCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCandidatePictureCommandHandler>.Instance);

            var firstResult = await handler.Handle(new UploadCandidatePictureCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("picture.png", "image/png", "image-content")
            }, CancellationToken.None);
            var previousFilePath = Path.Combine(root.Path, "uploads", "pictures", Path.GetFileName(firstResult!));

            var secondResult = await handler.Handle(new UploadCandidatePictureCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("picture-v2.png", "image/png", "image-content-v2")
            }, CancellationToken.None);

            secondResult.Should().NotBe(firstResult);
            File.Exists(previousFilePath).Should().BeFalse();
            File.Exists(Path.Combine(root.Path, "uploads", "pictures", Path.GetFileName(secondResult!))).Should().BeTrue();
        }

        [Fact]
        public async Task UploadCompanyLogo_ExistingProfile_SavesFileAndUrl()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CompanyProfile { UserId = "company-1" };
            context.CompanyProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCompanyLogoCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCompanyLogoCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCompanyLogoCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("logo.jpg", "image/jpeg", "logo-content")
            }, CancellationToken.None);

            result.Should().StartWith("/uploads/logos/");
            File.Exists(Path.Combine(root.Path, "uploads", "logos", Path.GetFileName(result))).Should().BeTrue();
        }

        [Fact]
        public async Task UploadCompanyLogo_MissingProfile_ReturnsNull()
        {
            using var context = TestHelpers.CreateDbContext();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCompanyLogoCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCompanyLogoCommandHandler>.Instance);

            var result = await handler.Handle(new UploadCompanyLogoCommand
            {
                Id = Guid.NewGuid(),
                File = TestHelpers.CreateFile("logo.jpg", "image/jpeg", "logo-content")
            }, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UploadCompanyLogo_ExistingLogo_DeletesPreviousFile()
        {
            using var context = TestHelpers.CreateDbContext();
            var profile = new CompanyProfile { UserId = "company-1" };
            context.CompanyProfiles.Add(profile);
            await context.SaveChangesAsync();
            using var root = TestHelpers.CreateTemporaryRoot();
            var handler = new UploadCompanyLogoCommandHandler(
                context,
                TestHelpers.CreateEnvironment(root.Path),
                NullLogger<UploadCompanyLogoCommandHandler>.Instance);

            var firstResult = await handler.Handle(new UploadCompanyLogoCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("logo.jpg", "image/jpeg", "logo-content")
            }, CancellationToken.None);
            var previousFilePath = Path.Combine(root.Path, "uploads", "logos", Path.GetFileName(firstResult!));

            var secondResult = await handler.Handle(new UploadCompanyLogoCommand
            {
                Id = profile.Id,
                File = TestHelpers.CreateFile("logo-v2.jpg", "image/jpeg", "logo-content-v2")
            }, CancellationToken.None);

            secondResult.Should().NotBe(firstResult);
            File.Exists(previousFilePath).Should().BeFalse();
            File.Exists(Path.Combine(root.Path, "uploads", "logos", Path.GetFileName(secondResult!))).Should().BeTrue();
        }

    }
}
