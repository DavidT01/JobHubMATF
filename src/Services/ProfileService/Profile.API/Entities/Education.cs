namespace Profile.API.Entities
{
    public class Education
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string InstitutionName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Degree { get; set; } = string.Empty;

        public Guid CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
    }
}
