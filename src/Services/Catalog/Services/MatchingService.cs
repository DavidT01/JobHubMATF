using System.Text.RegularExpressions;
using Catalog.DTOs;
using Catalog.Entities;

namespace Catalog.Services;

public class MatchingService : IMatchingService
{
    private const double SkillsWeight = 0.7;
    private const double ExperienceWeight = 0.3;
    
    //private static HashSet<string> ParseSkills(string skills)
    //{
      //  return skills.Split(',',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        //    .Select(s => s.ToLowerInvariant())
          //  .ToHashSet(); 
    //}
    
    private static double CalculateExperienceScore(List<ExperienceDto>  experience, ExperienceLevel jobLevel)
    {
        var years = CalculateYearsOfExperience(experience);
        var candidateLevel = YearsToLevel(years);

        if (candidateLevel == jobLevel)
            return 1.0;

        var diff = Math.Abs((int)candidateLevel - (int)jobLevel);
        return diff == 1 ? 0.5 : 0.0;
    }


    private static double CalculateYearsOfExperience(List<ExperienceDto> experience)
    {
        if (experience.Count == 0)
        {
            return 0;
        }

        var earliestStart = experience.Min(e => e.StartDate);
        var latestEnd = experience.Max(e => e.EndDate ?? DateTime.UtcNow);

        return (latestEnd - earliestStart).TotalDays / 365.25;
    }
    /* private static int ExtractYears(string text)
     {
         var match = Regex.Match(text, @"(\d+)\s*(godin|year)");
         return match.Success ? int.Parse(match.Groups[1].Value) : 0;
     }
     */
    
    private static ExperienceLevel YearsToLevel(double years) => years switch
    {
        < 2 => ExperienceLevel.Junior,
        < 5 => ExperienceLevel.Mid,
        < 8 => ExperienceLevel.Senior,
        _ => ExperienceLevel.Lead
    };
    
    public MatchResultDto CalculateMatch(Job job, CandidateProfileDto candidate)
    {
        var candidateSkills = candidate.Skills.Select(s => s.Trim().ToLowerInvariant()).ToHashSet();
        var jobSkills = job.Skills ?? new List<string>();
        
        var matched = jobSkills
            .Where(s => candidateSkills.Contains(s.Trim().ToLowerInvariant()))
            .ToList();
        var missing = jobSkills.Except(matched).ToList();
        
        double skillsScore = jobSkills.Count == 0
            ? 1.0
            : (double)matched.Count / jobSkills.Count;
        
        
        double experienceScore = CalculateExperienceScore(candidate.Experience, job.ExperienceLevel);

        double finalScore = (skillsScore * SkillsWeight + experienceScore * ExperienceWeight) * 100;

        return new MatchResultDto
        {
            JobId = job.Id,
            JobTitle = job.Title,
            Score = Math.Round(finalScore, 1),
            MatchedSkills = matched,
            MissingSkills = missing
        };
    
    }
}