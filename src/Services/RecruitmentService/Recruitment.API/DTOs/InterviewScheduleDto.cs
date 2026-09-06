namespace Recruitment.API.DTOs
{
    public class InterviewScheduleDto
    {
        public Guid Id { get; set; }
        public Guid SelectionRoundId { get; set; }
        public Guid CandidateProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] AdditionalAttendeeEmails { get; set; } = [];
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? GoogleMeetUrl { get; set; }
    }
}
