namespace Recruitment.API.Entities
{
    public class InterviewSchedule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SelectionRoundId { get; set; }
        public Guid CandidateProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] AdditionalAttendeeEmails { get; set; } = [];
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? GoogleMeetUrl { get; set; }
        public string? EventId { get; set; }

        public SelectionRound? SelectionRound { get; set; }
    }
}
