using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Queries.GetProcessByJobId
{
    public class GetProcessByJobIdQueryHandler(RecruitmentContext context, IMapper mapper, ILogger<GetProcessByJobIdQueryHandler> logger) : IRequestHandler<GetProcessByJobIdQuery, RecruitmentProcessDto?>
    {
        public async Task<RecruitmentProcessDto?> Handle(GetProcessByJobIdQuery request, CancellationToken cancellationToken)
        {
            var process = await context.Processes
                .Include(p => p.Rounds.OrderBy(r => r.Index))
                .FirstOrDefaultAsync(p => p.JobId == request.JobId, cancellationToken);

            if (process == null)
            {
                logger.LogWarning("Recruitment process for JobId {JobId} was not found.", request.JobId);
                return null;
            }

            logger.LogInformation("Retrieved recruitment process {ProcessId} for JobId {JobId}", process.Id, request.JobId);
            return mapper.Map<RecruitmentProcessDto>(process);
        }
    }
}
