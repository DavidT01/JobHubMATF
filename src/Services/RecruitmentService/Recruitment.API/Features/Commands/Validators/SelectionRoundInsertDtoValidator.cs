using FluentValidation;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.Validators;

public class SelectionRoundInsertDtoValidator : AbstractValidator<SelectionRoundInsertDto>
{
    public SelectionRoundInsertDtoValidator()
    {
        RuleFor(round => round.Title).NotEmpty().MaximumLength(200);
        RuleFor(round => round.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
