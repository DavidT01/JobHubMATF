using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.Entities;

namespace Recruitment.API.Features.Commands.UpdateSelectionRounds
{
    public class UpdateSelectionRoundsCommandHandler(RecruitmentContext context, IMapper mapper, ILogger<UpdateSelectionRoundsCommandHandler> logger) : IRequestHandler<UpdateSelectionRoundsCommand, bool>
    {
        public async Task<bool> Handle(UpdateSelectionRoundsCommand request, CancellationToken cancellationToken)
        {
            var process = await context.Processes
                .Include(p => p.Rounds)
                .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken);

            if (process == null)
            {
                logger.LogWarning("Recruitment process {ProcessId} was not found while updating selection rounds.", request.ProcessId);
                return false;
            }

            var requestIds = request.Rounds.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToList();
            var toRemove = process.Rounds.Where(r => !requestIds.Contains(r.Id)).ToList();
            context.Rounds.RemoveRange(toRemove);

            foreach(var round in request.Rounds)
            {
                if(round.Id.HasValue)
                {
                    var existing = process.Rounds.FirstOrDefault(r => r.Id == round.Id.Value);
                    if(existing != null)
                    {
                        mapper.Map(round, existing);
                        existing.ModifiedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var newRound = mapper.Map<SelectionRound>(round);
                    newRound.Id = Guid.NewGuid();
                    newRound.RecruitmentProcessId = process.Id;
                    newRound.CreatedAt = DateTime.UtcNow;
                    context.Rounds.Add(newRound);
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully updated selection rounds for recruitment process {ProcessId}", process.Id);
            return true;
        }
    }
}
