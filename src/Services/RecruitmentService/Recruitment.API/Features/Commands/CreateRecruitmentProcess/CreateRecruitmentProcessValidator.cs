using FluentValidation;

namespace Recruitment.API.Features.Commands.CreateRecruitmentProcess
{
    public class CreateRecruitmentProcessValidator : AbstractValidator<CreateRecruitmentProcessCommand>
    {
        public CreateRecruitmentProcessValidator()
        {
            RuleFor(x => x.CompanyId).NotEmpty();
            RuleFor(x => x.JobId).NotEmpty();
        }
    }
}
