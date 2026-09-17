namespace Catalog.DTOs;

public class CandidateProfileDto
{
    public string Id { get; set; } =  string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<ExperienceDto> Experience { get; set; } = new();
    public List<string> Skills { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}