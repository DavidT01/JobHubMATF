using FluentValidation;
using Profile.API.Features.CompanyProfiles.Commands.UploadLogo;

namespace Profile.API.Features.CompanyProfiles.Commands.Validators
{
    public class UploadCompanyLogoCommandValidator : AbstractValidator<UploadCompanyLogoCommand>
    {
        public UploadCompanyLogoCommandValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("Id is required.");

            RuleFor(p => p.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("File is required.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.")
                .Must(f => f.ContentType == "image/png" || f.ContentType == "image/jpg" || f.ContentType == "image/jpeg")
                    .WithMessage("Only png/jpg/jpeg files are allowed for Logo upload.");
        }
    }
}
