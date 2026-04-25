namespace Profile.API.Entities
{
    public class Language
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Level { get; set; } = string.Empty;

        public Guid CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
    }
}
