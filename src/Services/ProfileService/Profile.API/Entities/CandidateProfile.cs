namespace Profile.API.Entities
{
    public class CandidateProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<Education> Education { get; set; } = new();
        public List<Experience> Experience { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public List<Language> Languages { get; set; } = new();
        public string CvUrl { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string? GithubUrl { get; set; }
        public string? GitlabUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
