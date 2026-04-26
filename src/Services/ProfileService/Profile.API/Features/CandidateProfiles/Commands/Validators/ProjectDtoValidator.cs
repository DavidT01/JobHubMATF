using FluentValidation;
using Profile.API.DTOs;
using Profile.API.Extensions;

namespace Profile.API.Features.CandidateProfiles.Commands.Validators
{
    public class ProjectDtoValidator : AbstractValidator<ProjectDto>
    {
        public ProjectDtoValidator()
        {
            RuleFor(p => p.Name).NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(30).WithMessage("Project name cannot exceed 30 characters.");

            RuleFor(p => p.Description).MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");

            RuleFor(p => p.RepositoryUrl).ValidUrl().WithName("RepositoryUrl");
        }
    }
}
