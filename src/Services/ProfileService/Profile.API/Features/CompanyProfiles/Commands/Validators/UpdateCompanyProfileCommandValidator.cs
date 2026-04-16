using FluentValidation;
using Profile.API.Features.CompanyProfiles.Commands.UpdateCompany;

namespace Profile.API.Features.CompanyProfiles.Commands.Validators
{
    public class UpdateCompanyProfileCommandValidator : AbstractValidator<UpdateCompanyProfileCommand>
    {
        public UpdateCompanyProfileCommandValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("Profile Id is required.");

            RuleFor(p => p.UserId).NotEmpty().WithMessage("UserId is required.");

            RuleFor(p => p.CompanyName).NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(50).WithMessage("Company name cannot exceed 50 characters.");

            RuleFor(p => p.Location).MaximumLength(30).WithMessage("Location cannot exceed 30 characters.");

            RuleFor(p => p.ContactEmail).NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Invalid contact email format.");

            RuleFor(p => p.ContactPhone).MaximumLength(20).WithMessage("Contact phone cannot exceed 20 characters.");

            RuleFor(p => p.WebsiteUrl)
                .Must(BeAValidUrl).WithMessage("Website URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.WebsiteUrl));

            RuleFor(p => p.LinkedInUrl)
                .Must(BeAValidUrl).WithMessage("LinkedIn URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.LinkedInUrl));

            RuleFor(p => p.LogoUrl)
                .Must(BeAValidUrl).WithMessage("Logo URL must be a valid link.")
                .When(p => !string.IsNullOrEmpty(p.LogoUrl));
        }

        private bool BeAValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
