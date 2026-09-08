using FluentValidation;
using Profile.API.DTOs;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class EducationDtoValidator : AbstractValidator<EducationDto>
    {
        public EducationDtoValidator()
        {
            RuleFor(e => e.InstitutionName).NotEmpty().WithMessage("Institution name is required.")
                .MaximumLength(50).WithMessage("Institution name cannot exceed 50 characters.");

            RuleFor(e => e.Degree).MaximumLength(20).WithMessage("Degree cannot exceed 20 characters.");

            RuleFor(e => e.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Start date must be a valid date.");

            RuleFor(e => e.EndDate)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Start date must be a valid date.")
                .GreaterThan(e => e.StartDate).When(e => e.EndDate.HasValue).WithMessage("End date must be greater than Start date");
        }
    }
}
