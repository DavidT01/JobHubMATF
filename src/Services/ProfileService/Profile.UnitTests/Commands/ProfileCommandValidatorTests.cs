using FluentAssertions;
using Profile.API.DTOs;
using Profile.API.Features.CandidateProfiles.Commands.UploadCv;
using Profile.API.Features.CandidateProfiles.Commands.UploadPicture;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.Validators;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;
using Profile.API.Features.CompanyProfiles.Commands.UploadLogo;
using Profile.API.Features.CompanyProfiles.Commands.Validators;
using Profile.UnitTests.Common;

namespace Profile.UnitTests.Commands
{
    public class FeatureValidatorTests
    {
        [Fact]
        public void UpdateCandidateValidator_InvalidFields_ReturnsErrors()
        {
            var result = new UpdateCandidateProfileCommandValidator().Validate(new UpdateCandidateProfileCommand
            {
                Id = Guid.Empty,
                UserId = "",
                FirstName = "",
                LastName = "",
                Email = "invalid-email",
                GithubUrl = "ftp://github.com/user",
                Skills = [new string('x', 21)],
                Education = [new("", DateTime.UtcNow.AddDays(1), null, null)],
                Projects = [new("", new string('x', 251), "invalid")],
                Languages = [new("", new string('x', 16))]
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCandidateProfileCommand.Id));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCandidateProfileCommand.Email));
            result.Errors.Should().Contain(error => error.PropertyName == "Skills[0]");
            result.Errors.Should().Contain(error => error.PropertyName.StartsWith("Education[0]"));
            result.Errors.Should().Contain(error => error.PropertyName.StartsWith("Projects[0]"));
            result.Errors.Should().Contain(error => error.PropertyName.StartsWith("Languages[0]"));
        }

        [Fact]
        public void UpdateCompanyValidator_InvalidFields_ReturnsErrors()
        {
            var result = new UpdateCompanyProfileCommandValidator().Validate(new UpdateCompanyProfileCommand
            {
                Id = Guid.Empty,
                UserId = "",
                CompanyName = "",
                ContactEmail = "invalid-email",
                WebsiteUrl = "ftp://company.example",
                LinkedInUrl = "not-a-url",
                LogoUrl = "not-a-url"
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCompanyProfileCommand.Id));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateCompanyProfileCommand.ContactEmail));
            result.Errors.Should().Contain(error => error.PropertyName == "WebsiteUrl");
            result.Errors.Should().Contain(error => error.PropertyName == "LinkedInUrl");
            result.Errors.Should().Contain(error => error.PropertyName == "LogoUrl");
        }

        [Fact]
        public void UploadValidators_InvalidFiles_ReturnErrors()
        {
            var emptyFile = TestHelpers.CreateFile("empty.pdf", "application/pdf", Array.Empty<byte>());
            var invalidFile = TestHelpers.CreateFile("file.txt", "text/plain", [1]);

            var cvResult = new UploadCandidateCvCommandValidator().Validate(new UploadCandidateCvCommand { File = invalidFile });
            var pictureResult = new UploadCandidatePictureCommandValidator().Validate(new UploadCandidatePictureCommand { File = emptyFile });
            var logoResult = new UploadCompanyLogoCommandValidator().Validate(new UploadCompanyLogoCommand { File = null! });

            cvResult.IsValid.Should().BeFalse();
            pictureResult.IsValid.Should().BeFalse();
            logoResult.IsValid.Should().BeFalse();
        }

        [Fact]
        public void NestedValidators_InvalidValues_ReturnErrors()
        {
            var education = new EducationDtoValidator().Validate(new EducationDto("", DateTime.UtcNow.AddDays(1), DateTime.UtcNow, ""));
            var experience = new ExperienceDtoValidator().Validate(new ExperienceDto("", "", DateTime.UtcNow.AddDays(1), DateTime.UtcNow));
            var project = new ProjectDtoValidator().Validate(new ProjectDto("", new string('x', 251), "invalid"));
            var language = new LanguageDtoValidator().Validate(new LanguageDto("", new string('x', 16)));

            education.IsValid.Should().BeFalse();
            experience.IsValid.Should().BeFalse();
            project.IsValid.Should().BeFalse();
            language.IsValid.Should().BeFalse();
        }
    }
}
