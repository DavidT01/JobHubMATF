namespace Profile.API.DTOs;

public class CandidateProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<EducationDto> Education { get; set; } = new();
    public List<ExperienceDto> Experience { get; set; } = new();
    public List<ProjectDto> Projects { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public List<LanguageDto> Languages { get; set; } = new();
    public string CvUrl { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? GitlabUrl { get; set; }
    public string? LinkedInUrl { get; set; }
}
