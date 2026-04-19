using FluentValidation;
using Profile.API.Features.CandidateProfiles.Commands.UploadCv;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class UploadCandidateCvCommandValidator : AbstractValidator<UploadCandidateCvCommand>
    {
        public UploadCandidateCvCommandValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("Id is required.");

            RuleFor(p => p.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("File is required.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.")
                .Must(f => f.ContentType == "application/pdf").WithMessage("Only PDF files are allowed for CV upload.");
        }
    }
}
