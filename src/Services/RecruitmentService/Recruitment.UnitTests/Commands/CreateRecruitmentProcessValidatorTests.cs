using FluentAssertions;
using Recruitment.API.Features.Commands.CreateRecruitmentProcess;

namespace Recruitment.UnitTests.Commands
{
    public class CreateRecruitmentProcessValidatorTests
    {
        private readonly CreateRecruitmentProcessValidator _validator = new();

        [Fact]
        public void Validate_ValidCommand_HasNoErrors()
        {
            var command = new CreateRecruitmentProcessCommand { CompanyId = Guid.NewGuid(), JobId = "0123456789abcdef01234567" };

            var result = _validator.Validate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyCompanyId_HasError()
        {
            var command = new CreateRecruitmentProcessCommand { CompanyId = Guid.Empty, JobId = "0123456789abcdef01234567" };

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRecruitmentProcessCommand.CompanyId));
        }

        [Fact]
        public void Validate_EmptyJobId_HasError()
        {
            var command = new CreateRecruitmentProcessCommand { CompanyId = Guid.NewGuid(), JobId = string.Empty };

            var result = _validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRecruitmentProcessCommand.JobId));
        }
    }
}
