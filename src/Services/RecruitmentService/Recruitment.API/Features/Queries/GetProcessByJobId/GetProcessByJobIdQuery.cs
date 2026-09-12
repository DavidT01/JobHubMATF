using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetProcessByJobId
{
    public class GetProcessByJobIdQuery(string jobId) : IRequest<RecruitmentProcessDto?>
    {
        public string JobId { get; set; } = jobId;
    }
}
