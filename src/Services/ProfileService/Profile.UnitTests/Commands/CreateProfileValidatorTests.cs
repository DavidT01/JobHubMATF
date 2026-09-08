using FluentAssertions;
using Profile.UnitTests.Common;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;
using Profile.API.Features.CandidateProfiles.Commands.Validators;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;
using Profile.API.Features.CompanyProfiles.Commands.Validators;

namespace Profile.UnitTests.Commands
{
    public class ValidatorTests
    {
        [Fact]
        public void CreateCandidateValidator_EmptyRequiredFields_ReturnsErrors()
        {
            using var context = TestHelpers.CreateDbContext();
            var result = new CreateCandidateProfileCommandValidator(context).Validate(new CreateCandidateProfileCommand());

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCandidateProfileCommand.UserId));
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCandidateProfileCommand.Email));
        }

        [Fact]
        public async Task CreateCandidateValidator_ValidCommand_HasNoErrors()
        {
            using var context = TestHelpers.CreateDbContext();
            var result = await new CreateCandidateProfileCommandValidator(context).ValidateAsync(new CreateCandidateProfileCommand
            {
                UserId = "candidate-1",
                FirstName = "Ana",
                LastName = "Peric",
                Email = "ana@example.com"
            });

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCandidateValidator_UserIdAlreadyExists_ReturnsError()
        {
            using var context = TestHelpers.CreateDbContext();
            context.CandidateProfiles.Add(new Profile.API.Entities.CandidateProfile
            {
                UserId = "candidate-1",
                FirstName = "Ana",
                LastName = "Peric",
                Email = "ana@example.com"
            });
            await context.SaveChangesAsync();

            var result = await new CreateCandidateProfileCommandValidator(context).ValidateAsync(new CreateCandidateProfileCommand
            {
                UserId = "candidate-1",
                FirstName = "Marko",
                LastName = "Markovic",
                Email = "marko@example.com"
            });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCandidateProfileCommand.UserId));
        }

        [Fact]
        public async Task CreateCompanyValidator_ValidCommand_HasNoErrors()
        {
            using var context = TestHelpers.CreateDbContext();
            var result = await new CreateCompanyProfileCommandValidator(context).ValidateAsync(new CreateCompanyProfileCommand
            {
                UserId = "company-1",
                CompanyName = "Company",
                ContactEmail = "contact@company.example"
            });

            result.IsValid.Should().BeTrue();
        }
    }
}
