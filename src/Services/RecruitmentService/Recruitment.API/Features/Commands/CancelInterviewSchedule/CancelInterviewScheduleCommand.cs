using MediatR;

namespace Recruitment.API.Features.Commands.CancelInterviewSchedule;

public record CancelInterviewScheduleCommand(Guid InterviewScheduleId) : IRequest;
