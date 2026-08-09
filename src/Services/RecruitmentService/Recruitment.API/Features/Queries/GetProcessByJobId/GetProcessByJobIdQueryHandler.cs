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
            logger.LogInformation("Pokrenut upit GetProcessByJobIdQuery za JobId: {JobId}", request.JobId);

            var process = await context.Processes
                .Include(p => p.Rounds.OrderBy(r => r.Index))
                .FirstOrDefaultAsync(p => p.JobId == request.JobId, cancellationToken);

            if (process == null)
                return null;

            return mapper.Map<RecruitmentProcessDto>(process);
        }
    }
}
