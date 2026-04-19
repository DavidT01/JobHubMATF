using FluentValidation;
using Profile.API.Extensions;
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

            RuleFor(p => p.WebsiteUrl).ValidUrl().WithName("WebsiteUrl");

            RuleFor(p => p.LinkedInUrl).ValidUrl().WithName("LinkedInUrl");

            RuleFor(p => p.LogoUrl).ValidUrl().WithName("LogoUrl");
        }
    }
}
