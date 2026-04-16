using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.Features.CompanyProfiles.Commands.CreateCompany;

namespace Profile.API.Features.CompanyProfiles.Commands.Validators
{
    public class CreateCompanyProfileCommandValidator : AbstractValidator<CreateCompanyProfileCommand>
    {
        public CreateCompanyProfileCommandValidator(IProfileContext context)
        {
            RuleFor(p => p.UserId).NotEmpty().WithMessage("UserId is required.")
                .MustAsync(async (userId, cancellationToken) => !await context.CompanyProfiles.AnyAsync(p => p.UserId == userId, cancellationToken))
                .WithMessage("Company profile for this user already exists.");
        
            RuleFor(p => p.CompanyName).NotEmpty().WithMessage("Company name is required.")
                .MaximumLength(50).WithMessage("Company name cannot exceed 50 characters.");

            RuleFor(p => p.ContactEmail).NotEmpty().WithMessage("Contact email is required.")
                .EmailAddress().WithMessage("Invalid contact email format.");

            RuleFor(p => p.ContactPhone).MaximumLength(20).WithMessage("Contact phone cannot exceed 20 characters.");
        }
    }
}
