using Catalog.Entities;

namespace Catalog.Repositories;

public interface IJobRepository
{
    Task<IEnumerable<Job>> GetAllAsync();
    
    Task<Job?> GetByIdAsync(string id);
    
    Task CreateJobAsync(Job job);
    
    Task<bool> UpdateJobAsync(Job job);
    
    Task<bool> DeleteJobAsync(string id);
    
    Task<IEnumerable<Job>> SearchJobAsync(string query);
    
    Task<IEnumerable<Job>> FilterJobAsync(JobType? jobType , ExperienceLevel? experienceLevel , WorkMode? workMode, string? city);
    
    Task<IEnumerable<Job>> GetActiveJobsAsync();
    
    Task<IEnumerable<Job>> FilterBySalaryAsync(decimal? minSalary, decimal? maxSalary);
    
    Task<IEnumerable<Job>> GetByCompanyIdAsync(string companyId);
    
    Task<IEnumerable<Job>> GetSortedBySalaryAsync(bool ascending = true);
    
}