namespace Profile.API.Entities
{
    public class Experience
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Guid CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
    }
}
