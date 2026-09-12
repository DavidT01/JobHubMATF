using MediatR;

namespace Recruitment.API.Features.Commands.CreateRecruitmentProcess
{
    public class CreateRecruitmentProcessCommand : IRequest<Guid>
    {
        public Guid CompanyId { get; set; }
        public string JobId { get; set; } = string.Empty;
    }
}
