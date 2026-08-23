using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.ScheduleInterview
{
    public class ScheduleInterviewCommand : IRequest<InterviewScheduleDto>
    {
        public Guid SelectionRoundId { get; set; }
        public Guid CandidateProfileId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] AttendeeEmails { get; set; } = Array.Empty<string>();
    }
}
