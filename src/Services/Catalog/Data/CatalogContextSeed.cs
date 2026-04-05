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
                Description = "Develop and maintain web applications",
                CompanyId = "company-1",
                CompanyName = "TechCorp",
                Salary = 120000,
                Location = "Belgrade",
                PostedDate = DateTime.UtcNow
            },
            new Job
            {
                Title = "Data Analyst",
                Description = "Analyze datasets and generate insights",
                CompanyId = "company-2",
                CompanyName = "DataWorks",
                Salary = 90000,
                Location = "Novi Sad",
                PostedDate = DateTime.UtcNow
            },
            new Job
            {
                Title = "Frontend Developer",
                Description = "Build UI components for web apps",
                CompanyId = "company-3",
                CompanyName = "WebSolutions",
                Salary = 100000,
                Location = "Niš",
                PostedDate = DateTime.UtcNow
            }
        };
    }
}