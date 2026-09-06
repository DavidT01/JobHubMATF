using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.UpdateInterviewSchedule;

public record UpdateInterviewScheduleCommand : IRequest<InterviewScheduleDto>
{
    public Guid InterviewScheduleId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] AdditionalAttendeeEmails { get; init; } = [];
}
