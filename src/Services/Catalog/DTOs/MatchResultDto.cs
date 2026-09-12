namespace Catalog.DTOs;

public class MatchResultDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
}