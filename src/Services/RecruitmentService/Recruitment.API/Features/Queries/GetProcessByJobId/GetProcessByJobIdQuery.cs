using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetProcessByJobId
{
    public class GetProcessByJobIdQuery(Guid jobId) : IRequest<RecruitmentProcessDto?>
    {
        public Guid JobId { get; set; } = jobId;
    }
}
