using FluentAssertions;
using Recruitment.API.Features.Commands.ActivateCandidateProgress;

namespace Recruitment.UnitTests.Commands;

public sealed class ActivateCandidateProgressCommandValidatorTests
{
    private readonly ActivateCandidateProgressCommandValidator validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var result = validator.Validate(new ActivateCandidateProgressCommand(
            Guid.NewGuid(), "0123456789abcdef01234567"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidIdentifiers_ReturnsErrors()
    {
        var result = validator.Validate(new ActivateCandidateProgressCommand(
            Guid.Empty, "not-a-catalog-job"));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should().Contain(["CandidateProfileId", "JobId"]);
    }
}
