using FluentValidation;
using Profile.API.Extensions;
using Profile.API.Features.CandidateProfiles.Commands.UploadPicture;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class UploadCandidatePictureCommandValidator : AbstractValidator<UploadCandidatePictureCommand>
    {
        public UploadCandidatePictureCommandValidator()
        {
            RuleFor(p => p.Id).NotEmpty().WithMessage("Id is required.");

            RuleFor(p => p.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("File is required.")
                .Must(f => f.Length > 0).WithMessage("File cannot be empty.")
                .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.")
                .ValidImage();
        }
    }
}
