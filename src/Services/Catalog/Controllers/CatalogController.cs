using System.Text.Json;
using Catalog.Clients;
using Catalog.DTOs;
using Catalog.Entities;
using Catalog.Repositories;
using Catalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IJobRepository _repository;
    private readonly IMatchingService _matchingService;
    private readonly IProfileApiClient _profileApiClient;
    private readonly IBookmarkRepository _bookmarkRepository;
    private readonly IDistributedCache _cache;
    public CatalogController(IJobRepository repository, 
                             IMatchingService matchingService, 
                             IProfileApiClient profileApiClient ,
                             IBookmarkRepository  bookmarkRepository,
                             IDistributedCache cache)
    {
        _repository = repository;
        _matchingService = matchingService;
        _profileApiClient = profileApiClient;
        _bookmarkRepository = bookmarkRepository;
        _cache = cache;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
    {
        const string cacheKey = "allJobs";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return Ok(JsonSerializer.Deserialize<List<Job>>(cached));
        }
        
        var jobs = await _repository.GetAllAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(jobs),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetById(string id)
    {
        var job = await _repository.GetByIdAsync(id);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Job),StatusCodes.Status201Created)]
    public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
    {
        await _repository.CreateJobAsync(job);
        
        await _cache.RemoveAsync("allJobs");
        await _cache.RemoveAsync("activeJobs");
        
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Job), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Job>> UpdateJob(string id, [FromBody] Job job)
    {
        if (id != job.Id)
            return BadRequest("Route id does not match job id in body.");

        var result = await _repository.UpdateJobAsync(job);
        if (!result)
            return NotFound();
        
        await _cache.RemoveAsync("allJobs");
        await _cache.RemoveAsync("activeJobs");
        
        return Ok(job);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteJob(string id)
    {
        var result = await _repository.DeleteJobAsync(id);
        if (!result)
            return NotFound(null);
        
        await _cache.RemoveAsync("allJobs");
        await _cache.RemoveAsync("activeJobs");
        
        return Ok();
    }
    
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Job>>> SearchJob([FromQuery] string query)
    {
        var jobs = await _repository.SearchJobAsync(query);
        return Ok(jobs);
    }
    
    [HttpGet("filter")]
    public async Task<ActionResult<IEnumerable<Job>>> FilterJob(
        [FromQuery] JobType? jobType,
        [FromQuery] ExperienceLevel? experienceLevel,
        [FromQuery] WorkMode? workMode,[FromQuery] string? city)   
    {
        var jobs = await _repository.FilterJobAsync(jobType, experienceLevel, workMode, city);
        return Ok(jobs);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<Job>>> GetActiveJobs()
    {
        const string cacheKey = "activeJobs";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return Ok(JsonSerializer.Deserialize<List<Job>>(cached));
        }
        
        var jobs = await _repository.GetActiveJobsAsync();

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(jobs),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        
        return Ok(jobs);
    }
    
    
    [HttpGet("filter/salary")]
    public async Task<ActionResult<IEnumerable<Job>>> FilterBySalary(
        [FromQuery] decimal? minSalary,
        [FromQuery] decimal? maxSalary)
    {
        var jobs = await _repository.FilterBySalaryAsync(minSalary, maxSalary);
        return Ok(jobs);
    }
    
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetByCompanyId(string companyId)
    {
        var jobs = await _repository.GetByCompanyIdAsync(companyId);
        return Ok(jobs);
    }
    
    
    [HttpGet("sorted/salary")]
    public async Task<ActionResult<IEnumerable<Job>>> GetSortedBySalary([FromQuery] bool ascending = true)
    {
        var jobs = await _repository.GetSortedBySalaryAsync(ascending);
        return Ok(jobs);
    }
    
    [HttpGet("match/{jobId}/{userId}")]
    public async Task<IActionResult> MatchJobToCandidate(string jobId, string userId)
    {
        var job = await _repository.GetByIdAsync(jobId);
        if (job == null)
            return NotFound("Job did not found");

        var candidateCacheKey = $"candidate:{userId}";
        var cachedCandidate = await _cache.GetStringAsync(candidateCacheKey);
        CandidateProfileDto? candidate;
        
        if (cachedCandidate != null)
        {
            candidate = JsonSerializer.Deserialize<CandidateProfileDto>(cachedCandidate);    
        }
        else
        {
            candidate = await _profileApiClient.GetCandidateByIdAsync(userId);
            if (candidate != null)
            {
                await _cache.SetStringAsync(candidateCacheKey, JsonSerializer.Serialize(candidate),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    });
            }
        }

        if (candidate == null)
            return NotFound("Candidate did not found");

        var result = _matchingService.CalculateMatch(job, candidate);
        return Ok(result);
    }
    
    [HttpPost("bookmarks")]
    public async Task<IActionResult> AddBookmark([FromQuery] string userId, [FromQuery] string jobId)
    {
        var alreadyExists = await _bookmarkRepository.IsBookmarkedAsync(userId, jobId);
        if (alreadyExists)
            return Conflict("Bookmark already exists");
        
        await _bookmarkRepository.AddAsync(userId, jobId);
        return Ok();
    }
    
    [HttpDelete("bookmarks")]
    public async Task<IActionResult> RemoveBookmark([FromQuery] string userId, [FromQuery] string jobId)
    {
        var result = await _bookmarkRepository.RemoveAsync(userId, jobId);
        if (!result)
            return NotFound();
        return Ok();
    }
    
    [HttpGet("bookmarks/{userId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetBookmarkedJobs(string userId)
    {
        var bookmarks = await _bookmarkRepository.GetByUserIdAsync(userId);
        var jobIds = bookmarks.Select(b => b.JobId);

        var jobs = new List<Job>();
        foreach (var jobId in jobIds)
        {
            var job = await _repository.GetByIdAsync(jobId);
            if (job != null)
                jobs.Add(job);
        }

        return Ok(jobs);
    }


}