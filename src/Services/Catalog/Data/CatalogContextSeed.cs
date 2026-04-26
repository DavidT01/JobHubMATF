using Catalog.Entities;
using MongoDB.Driver;

namespace Catalog.Data;

public class CatalogContextSeed
{
    public static void SeedData(IMongoCollection<Job> jobCollection)
    {
        var existingJobs = jobCollection.Find(j => true).Any();
        if (!existingJobs)
        {
            jobCollection.InsertMany(GetConfiguredJobs());
        }
    }

    private static IEnumerable<Job> GetConfiguredJobs()
    {
        return new List<Job>
        {
            new Job
            {
                Title = "Software Engineer",
                Description = "Develop and maintain backend services",
                CompanyId = "company-1",
                CompanyName = "TechCorp",

                JobType = JobType.FullTime,
                ExperienceLevel = ExperienceLevel.Mid,
                WorkMode = WorkMode.Hybrid,

                SalaryMin = 1500,
                SalaryMax = 2500,
                Currency = "EUR",

                City = "Belgrade",
                Country = "Serbia",

                Skills = new List<string> { "C#", ".NET", "SQL" },
                Requirements = new List<string> { "2+ years experience", "OOP knowledge" },
                Responsibilities = new List<string> { "Develop APIs", "Maintain services" },

                PostedDate = DateTime.UtcNow,
                IsActive = true
            },

            new Job
            {
                Title = "Data Analyst",
                Description = "Analyze datasets and generate insights",
                CompanyId = "company-2",
                CompanyName = "DataWorks",

                JobType = JobType.FullTime,
                ExperienceLevel = ExperienceLevel.Junior,
                WorkMode = WorkMode.OnSite,

                SalaryMin = 1000,
                SalaryMax = 1800,
                Currency = "EUR",

                City = "Novi Sad",
                Country = "Serbia",

                Skills = new List<string> { "Python", "SQL", "Excel" },
                Requirements = new List<string> { "Statistics knowledge", "Data visualization" },
                Responsibilities = new List<string> { "Analyze data", "Create reports" },

                PostedDate = DateTime.UtcNow,
                IsActive = true
            },

            new Job
            {
                Title = "Frontend Developer",
                Description = "Build UI components for web apps",
                CompanyId = "company-3",
                CompanyName = "WebSolutions",

                JobType = JobType.Contract,
                ExperienceLevel = ExperienceLevel.Senior,
                WorkMode = WorkMode.Remote,

                SalaryMin = 2000,
                SalaryMax = 3500,
                Currency = "EUR",

                City = "Niš",
                Country = "Serbia",

                Skills = new List<string> { "React", "JavaScript", "CSS" },
                Requirements = new List<string> { "3+ years experience", "Frontend frameworks" },
                Responsibilities = new List<string> { "Build UI", "Optimize performance" },

                PostedDate = DateTime.UtcNow,
                IsActive = true
            }
        };
    }
    
}