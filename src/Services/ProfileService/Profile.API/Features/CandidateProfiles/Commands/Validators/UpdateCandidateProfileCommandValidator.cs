using FluentValidation;
using Profile.API.Extensions;
using Profile.API.Features.CandidateProfiles.Commands.UpdateCandidate;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class UpdateCandidateProfileCommandValidator : AbstractValidator<UpdateCandidateProfileCommand>
    {
        public UpdateCandidateProfileCommandValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("Profile Id is required.");

            RuleFor(p => p.UserId).NotEmpty().WithMessage("UserId is required.");

            RuleFor(p => p.FirstName).NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(p => p.LastName).NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(p => p.Email).NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(p => p.PhoneNumber).MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

            RuleFor(p => p.Location).MaximumLength(30).WithMessage("Location cannot exceed 30 characters.");

            RuleFor(p => p.GithubUrl).MustBeValidUrl().WithName("GithubUrl");

            RuleFor(p => p.GitlabUrl).MustBeValidUrl().WithName("GitlabUrl");

            RuleFor(p => p.LinkedInUrl).MustBeValidUrl().WithName("LinkedInUrl");

            RuleFor(p => p.CvUrl).MustBeValidUrl().WithName("CvUrl");
        }
    }
}
