using FluentValidation;

namespace Recruitment.API.Features.Commands.ActivateCandidateProgress;

public sealed class ActivateCandidateProgressCommandValidator
    : AbstractValidator<ActivateCandidateProgressCommand>
{
    public ActivateCandidateProgressCommandValidator()
    {
        RuleFor(command => command.CandidateProfileId)
            .NotEmpty();

        RuleFor(command => command.JobId)
            .NotEmpty()
            .Length(24)
            .Matches("^[0-9a-fA-F]+$");
    }
}