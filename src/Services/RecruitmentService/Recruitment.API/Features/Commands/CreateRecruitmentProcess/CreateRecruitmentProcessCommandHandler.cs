using AutoMapper;
using MediatR;
using Recruitment.API.Data;
using Recruitment.API.Entities;

namespace Recruitment.API.Features.Commands.CreateRecruitmentProcess
{
    public class CreateRecruitmentProcessCommandHandler(RecruitmentContext context, IMapper mapper, ILogger<CreateRecruitmentProcessCommandHandler> logger) : IRequestHandler<CreateRecruitmentProcessCommand, Guid>
    {
        public async Task<Guid> Handle(CreateRecruitmentProcessCommand request, CancellationToken cancellationToken)
        {
            var process = mapper.Map<RecruitmentProcess>(request);
            process.Id = Guid.NewGuid();
            process.Active = false;
            process.CreatedAt = DateTime.UtcNow;

            context.Processes.Add(process);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created recruitment process with Id {ProcessId} for Job {JobId}", process.Id, request.JobId);

            return process.Id;
        }
    }
}
