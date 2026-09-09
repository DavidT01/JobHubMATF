namespace Profile.API.Entities
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RepositoryLink { get; set; }

        public Guid CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
    }
}
