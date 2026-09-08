using FluentValidation;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class LanguageDtoValidator : AbstractValidator<LanguageDto>
    {
        public LanguageDtoValidator()
        {
            RuleFor(l => l.Name).NotEmpty().WithMessage("Language name is required.")
                .MaximumLength(30).WithMessage("Language name cannot exceed 30 characters.");

            RuleFor(l => l.Level).MaximumLength(15).WithMessage("Level cannot exceed 15 characters.");
        }
    }
}
