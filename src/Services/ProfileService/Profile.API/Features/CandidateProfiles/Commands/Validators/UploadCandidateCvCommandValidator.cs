using FluentValidation;
using Profile.API.Extensions;
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
                .Must(f => f.Length > 0).WithMessage("File cannot be empty.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.")
                .ValidPdf();
        }
    }
}
