using Catalog.DTOs;
using Catalog.Entities;
using Catalog.Services;
using Xunit;

namespace Catalog.Tests;

public sealed class MatchingServiceTests
{
    private readonly MatchingService service = new();

    private static Job CreateJob(List<string> skills, ExperienceLevel level) => new()
    {
        Id = "job-1",
        Title = "Backend Developer",
        Skills = skills,
        ExperienceLevel = level
    };

    private static CandidateProfileDto CreateCandidate(List<string> skills, double yearsOfExperience)
    {
        var experience = yearsOfExperience <= 0
            ? new List<ExperienceDto>()
            : new List<ExperienceDto>
            {
                new()
                {
                    StartDate = DateTime.UtcNow.AddDays(-yearsOfExperience * 365.25),
                    EndDate = null
                }
            };

        return new CandidateProfileDto { Skills = skills, Experience = experience };
    }

    [Fact]
    public void All_skills_match_and_experience_level_is_exact_gives_full_score()
    {
        var job = CreateJob(["C#", "SQL"], ExperienceLevel.Mid);
        var candidate = CreateCandidate(["C#", "SQL"], yearsOfExperience: 3);

        var result = service.CalculateMatch(job, candidate);

        Assert.Equal(100.0, result.Score);
    }

    [Fact]
    public void No_matching_skills_and_experience_far_off_gives_zero_score()
    {
        var job = CreateJob(["C#", "SQL"], ExperienceLevel.Junior);
        var candidate = CreateCandidate(["Java"], yearsOfExperience: 10);

        var result = service.CalculateMatch(job, candidate);

        Assert.Equal(0.0, result.Score);
    }

    [Fact]
    public void Job_with_no_required_skills_treats_skills_score_as_full_match()
    {
        var job = CreateJob([], ExperienceLevel.Mid);
        var candidate = CreateCandidate([], yearsOfExperience: 3);

        var result = service.CalculateMatch(job, candidate);

        Assert.Equal(100.0, result.Score);
    }

    [Fact]
    public void Partial_skill_match_gives_proportional_score()
    {
        var job = CreateJob(["C#", "SQL", "Docker", "Angular"], ExperienceLevel.Mid);
        var candidate = CreateCandidate(["C#", "SQL"], yearsOfExperience: 3);

        var result = service.CalculateMatch(job, candidate);

        
        Assert.Equal(65.0, result.Score);
    }

    [Theory]
    [InlineData(1)] 
    [InlineData(6)] 
    public void Experience_one_level_off_gives_partial_credit(double years)
    {
        var job = CreateJob(["C#"], ExperienceLevel.Mid);
        var candidate = CreateCandidate(["C#"], yearsOfExperience: years);

        var result = service.CalculateMatch(job, candidate);
        
        Assert.Equal(85.0, result.Score);
    }

    [Fact]
    public void Experience_two_or_more_levels_off_gives_no_credit()
    {
        var job = CreateJob(["C#"], ExperienceLevel.Lead);
        var candidate = CreateCandidate(["C#"], yearsOfExperience: 1); 

        var result = service.CalculateMatch(job, candidate);
        
        Assert.Equal(70.0, result.Score);
    }

    [Fact]
    public void Matched_and_missing_skills_are_reported_correctly()
    {
        var job = CreateJob(["C#", "SQL", "Docker"], ExperienceLevel.Junior);
        var candidate = CreateCandidate(["c#", "angular"], yearsOfExperience: 0);

        var result = service.CalculateMatch(job, candidate);

        Assert.Equal(["C#"], result.MatchedSkills);
        Assert.Equal(["SQL", "Docker"], result.MissingSkills);
    }

    [Fact]
    public void Skill_matching_is_case_insensitive()
    {
        var job = CreateJob(["C#"], ExperienceLevel.Junior);
        var candidate = CreateCandidate(["c#"], yearsOfExperience: 0);

        var result = service.CalculateMatch(job, candidate);

        Assert.Contains("C#", result.MatchedSkills);
        Assert.Empty(result.MissingSkills);
    }

    [Fact]
    public void Candidate_with_no_experience_entries_is_treated_as_junior()
    {
        var job = CreateJob(["C#"], ExperienceLevel.Junior);
        var candidate = CreateCandidate(["C#"], yearsOfExperience: 0);

        var result = service.CalculateMatch(job, candidate);

        Assert.Equal(100.0, result.Score);
    }
}
