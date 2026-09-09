using FluentValidation;
using Recruitment.API.DTOs;
using Recruitment.API.Features.Commands.UpdateSelectionRounds;

namespace Recruitment.API.Features.Commands.Validators;

public class UpdateSelectionRoundsCommandValidator : AbstractValidator<UpdateSelectionRoundsCommand>
{
    public UpdateSelectionRoundsCommandValidator()
    {
        RuleFor(command => command.ProcessId).NotEmpty();
        RuleForEach(command => command.Rounds).SetValidator(new SelectionRoundInsertDtoValidator());
        RuleFor(command => command.Rounds)
            .Must(HaveUniqueTitles)
            .WithMessage("Selection round titles must be unique within a recruitment process.");
    }

    private static bool HaveUniqueTitles(IEnumerable<SelectionRoundInsertDto> rounds)
    {
        return rounds
            .Select(round => round.Title?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == rounds.Count();
    }
}
