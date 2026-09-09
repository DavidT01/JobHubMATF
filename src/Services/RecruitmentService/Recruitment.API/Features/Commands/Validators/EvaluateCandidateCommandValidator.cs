using FluentValidation;
using Recruitment.API.Features.Commands.EvaluateCandidate;

namespace Recruitment.API.Features.Commands.Validators;

public class EvaluateCandidateCommandValidator : AbstractValidator<EvaluateCandidateCommand>
{
    public EvaluateCandidateCommandValidator()
    {
        RuleFor(command => command.CandidateProfileId).NotEmpty();
        RuleFor(command => command.SelectionRoundId).NotEmpty();
        RuleFor(command => command.Score).InclusiveBetween(1, 10);
    }
}
