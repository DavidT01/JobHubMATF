using FluentValidation;
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

            RuleFor(p => p.GithubUrl)
                .Must(BeAValidUrl).WithMessage("GitHub URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.GithubUrl));

            RuleFor(p => p.GitlabUrl)
                .Must(BeAValidUrl).WithMessage("GitLab URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.GitlabUrl));

            RuleFor(p => p.LinkedInUrl)
                .Must(BeAValidUrl).WithMessage("LinkedIn URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.LinkedInUrl));

            RuleFor(p => p.CvUrl)
                .Must(BeAValidUrl).WithMessage("CV URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.CvUrl));
        }

        private bool BeAValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
