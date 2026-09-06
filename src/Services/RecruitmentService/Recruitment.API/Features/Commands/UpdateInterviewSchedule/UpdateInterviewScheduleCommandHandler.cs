using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Recruitment.API.Data;
using Recruitment.API.DTOs;
using Recruitment.API.Exceptions;
using Recruitment.API.Infrastructure;

namespace Recruitment.API.Features.Commands.UpdateInterviewSchedule;

public class UpdateInterviewScheduleCommandHandler(
    RecruitmentContext context,
    IMapper mapper,
    IMeetingService meetingService,
    IProfileServiceClient profileServiceClient,
    ILogger<UpdateInterviewScheduleCommandHandler> logger) : IRequestHandler<UpdateInterviewScheduleCommand, InterviewScheduleDto>
{
    public async Task<InterviewScheduleDto> Handle(UpdateInterviewScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await context.InterviewSchedules.FirstOrDefaultAsync(
            schedule => schedule.Id == request.InterviewScheduleId,
            cancellationToken);
        if (schedule is null)
        {
            logger.LogWarning("Interview schedule {InterviewScheduleId} was not found.", request.InterviewScheduleId);
            throw new RecruitmentValidationException($"Interview schedule {request.InterviewScheduleId} not found.");
        }

        var hasConflict = await context.InterviewSchedules.AnyAsync(otherSchedule =>
            otherSchedule.Id != schedule.Id
            && otherSchedule.CandidateProfileId == schedule.CandidateProfileId
            && request.StartTime < otherSchedule.EndTime
            && request.EndTime > otherSchedule.StartTime,
            cancellationToken);
        if (hasConflict)
        {
            logger.LogWarning("Candidate {CandidateProfileId} already has an interview during the requested time.", schedule.CandidateProfileId);
            throw new RecruitmentValidationException("The candidate already has an interview during this time.");
        }

        var candidateContact = await profileServiceClient.GetCandidateContactAsync(schedule.CandidateProfileId, cancellationToken);
        var attendeeEmails = new[] { candidateContact.Email }
            .Concat(request.AdditionalAttendeeEmails)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!string.IsNullOrEmpty(schedule.EventId))
        {
            await meetingService.UpdateMeetingAsync(
                schedule.EventId,
                request.Title,
                request.Description,
                request.StartTime,
                request.EndTime,
                attendeeEmails);
        }

        mapper.Map(request, schedule);
        schedule.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully updated interview schedule {InterviewScheduleId}.", schedule.Id);
        return mapper.Map<InterviewScheduleDto>(schedule);
    }
}
