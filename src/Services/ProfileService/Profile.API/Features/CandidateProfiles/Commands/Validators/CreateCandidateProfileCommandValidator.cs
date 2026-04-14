using FluentValidation;
using Profile.API.Features.CandidateProfiles.Commands.CreateCandidate;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class CreateCandidateProfileCommandValidator : AbstractValidator<CreateCandidateProfileCommand>
    {
        public CreateCandidateProfileCommandValidator()
        {
            RuleFor(p => p.UserId).NotEmpty().WithMessage("UserId is required.");

            RuleFor(p => p.FirstName).NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(p => p.LastName).NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(p => p.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(p => p.PhoneNumber).MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");
        }
    }
}
