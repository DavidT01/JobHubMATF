using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.CancelInterviewSchedule;

public class CancelInterviewScheduleCommandHandler(
    RecruitmentContext context,
    IMeetingService meetingService,
    ILogger<CancelInterviewScheduleCommandHandler> logger) : IRequestHandler<CancelInterviewScheduleCommand>
{
    public async Task Handle(CancelInterviewScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await context.InterviewSchedules.FirstOrDefaultAsync(
            schedule => schedule.Id == request.InterviewScheduleId,
            cancellationToken);
        if (schedule is null)
        {
            logger.LogWarning("Interview schedule {InterviewScheduleId} was not found for cancellation.", request.InterviewScheduleId);
            throw new RecruitmentValidationException($"Interview schedule {request.InterviewScheduleId} not found.");
        }

        if (!string.IsNullOrEmpty(schedule.EventId))
        {
            await meetingService.DeleteMeetingAsync(schedule.EventId);
        }

        context.InterviewSchedules.Remove(schedule);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully cancelled interview schedule {InterviewScheduleId}.", request.InterviewScheduleId);
    }
}
