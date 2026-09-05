using Catalog.Data;
using Catalog.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Catalog.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ICatalogContext _context;
    public JobRepository(ICatalogContext catalogContext)
    {
        _context = catalogContext ??  throw new ArgumentNullException(nameof(catalogContext));
    }

    public async Task<IEnumerable<Job>> GetAllAsync()
    {
        return await _context.Jobs.Find(j => true).ToListAsync();
    }

    public async Task<Job?> GetByIdAsync(string id)
    {
        return await _context.Jobs.Find(j => j.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateJobAsync(Job job)
    { 
        await _context.Jobs.InsertOneAsync(job);
    }

    public async Task<bool> UpdateJobAsync(Job job)
    {
        var updateJob = await _context.Jobs.ReplaceOneAsync(j => j.Id == job.Id, job);
        return updateJob.ModifiedCount > 0;
    }

    public async Task<bool> DeleteJobAsync(string id)
    {
        var deleteJob = await _context.Jobs.DeleteOneAsync(j => j.Id == id);
        return deleteJob.DeletedCount > 0;
    }
    
    public async Task<IEnumerable<Job>> SearchJobAsync(string query)
    { 
        var filter =
            Builders<Job>.Filter.Or(
                Builders<Job>.Filter.Regex(
                    j => j.Title,
                    new MongoDB.Bson.BsonRegularExpression(query, "i")
                ),

                Builders<Job>.Filter.Regex(
                    j => j.Description,
                    new MongoDB.Bson.BsonRegularExpression(query, "i")
                ),

                Builders<Job>.Filter.Regex(
                    j => j.CompanyName,
                    new MongoDB.Bson.BsonRegularExpression(query, "i")
                )
            );

        return await _context
            .Jobs
            .Find(filter)
            .ToListAsync();
    }


    public async Task<IEnumerable<Job>> FilterJobAsync(JobType? jobType, ExperienceLevel? experienceLevel,
            WorkMode? workMode, string? city)
    {
        var filterBuilder = Builders<Job>.Filter;
        var filters = new List<FilterDefinition<Job>>();
        
        if (jobType.HasValue)
        {
            filters.Add(
                filterBuilder.Eq(j => j.JobType, jobType.Value)
            );
        }

        if (experienceLevel.HasValue)
        {
            filters.Add(
                filterBuilder.Eq(
                    j => j.ExperienceLevel,
                    experienceLevel.Value
                )
            );
        }

        if (workMode.HasValue)
        {
            filters.Add(
                filterBuilder.Eq(
                    j => j.WorkMode,
                    workMode.Value
                )
            );
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            filters.Add(
                filterBuilder.Regex(
                    j => j.City, new BsonRegularExpression($"^{city}$", "i")
                    )
                );
        }

        var finalFilter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        return await _context
            .Jobs
            .Find(finalFilter)
            .ToListAsync();
    }
    
     
    public async Task<IEnumerable<Job>> GetActiveJobsAsync()
    { 
        return await _context.Jobs.Find(j => j.IsActive).ToListAsync();
    }
        
     
    public async Task<IEnumerable<Job>> FilterBySalaryAsync(decimal? minSalary, decimal? maxSalary)
    {
        var filterBuilder = Builders<Job>.Filter;
        var filters = new List<FilterDefinition<Job>>();

        if (minSalary.HasValue)
        {
            filters.Add(filterBuilder.Gte(j => j.SalaryMax, minSalary.Value));
        }

        if (maxSalary.HasValue)
        {
            filters.Add(filterBuilder.Lte(j => j.SalaryMin, maxSalary.Value));
        }

        var finalFilter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        return await _context.Jobs.Find(finalFilter).ToListAsync();
    }
    
    
    public async Task<IEnumerable<Job>> GetByCompanyIdAsync(string companyId)
    { 
        return await _context.Jobs.Find(j => j.CompanyId == companyId).ToListAsync();
    }
    
    
    public async Task<IEnumerable<Job>> GetSortedBySalaryAsync(bool ascending = true)
    { 
        var sort = ascending 
            ? Builders<Job>.Sort.Ascending(j => j.SalaryMin) 
            : Builders<Job>.Sort.Descending(j => j.SalaryMin);
        
        return await _context.Jobs.Find(j => true).Sort(sort).ToListAsync();
    }
}