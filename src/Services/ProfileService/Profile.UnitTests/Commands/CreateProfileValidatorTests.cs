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
