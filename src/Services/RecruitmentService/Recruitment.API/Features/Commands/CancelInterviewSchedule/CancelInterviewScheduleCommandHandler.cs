using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.Exceptions;
using Recruitment.API.Features.Events;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.CancelInterviewSchedule;

public class CancelInterviewScheduleCommandHandler(
    RecruitmentContext context,
    IMeetingService meetingService,
    IProfileServiceClient profileServiceClient,
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

        var candidateProfile = await profileServiceClient.GetCandidateProfileAsync(
            schedule.CandidateProfileId, cancellationToken);
        var cancelledEvent = InterviewLifecycleEvent.Cancelled(schedule, candidateProfile.UserId);
        context.InterviewSchedules.Remove(schedule);
        context.OutboxMessages.Add(Data.Outbox.OutboxMessage.Create(cancelledEvent));
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Successfully cancelled interview schedule {InterviewScheduleId}.", request.InterviewScheduleId);
    }
}
