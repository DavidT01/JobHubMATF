using FluentValidation;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class ExperienceDtoValidator : AbstractValidator<ExperienceDto>
    {
        public ExperienceDtoValidator()
        {
            RuleFor(e => e.CompanyName).NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(50).WithMessage("Company name cannot exceed 50 characters.");

            RuleFor(e => e.Position).NotEmpty().WithMessage("Position is required.")
                .MaximumLength(30).WithMessage("Position cannot exceed 30 characters.");

            RuleFor(e => e.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Start date must be a valid date.");

            RuleFor(e => e.EndDate)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Start date must be a valid date.")
                .GreaterThan(e => e.StartDate).When(e => e.EndDate.HasValue).WithMessage("End date must be greater than Start date");
        }
    }
}
