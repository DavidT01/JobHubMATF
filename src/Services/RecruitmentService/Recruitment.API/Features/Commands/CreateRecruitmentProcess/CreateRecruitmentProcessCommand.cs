using MediatR;

namespace Recruitment.API.Features.Commands.CreateRecruitmentProcess
{
    public class CreateRecruitmentProcessCommand : IRequest<Guid>
    {
        public Guid CompanyId { get; set; }
        public Guid JobId { get; set; }
    }
}
