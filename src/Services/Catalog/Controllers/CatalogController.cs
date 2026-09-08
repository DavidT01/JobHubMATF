using Catalog.Entities;
using Catalog.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IJobRepository _repository;
    public CatalogController(IJobRepository repository)
    {
        _repository = repository;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Job>>> GetAllJobs()
    {
        var jobs = await _repository.GetAllAsync();
        
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
        var jobs = await _repository.GetActiveJobsAsync();
        return Ok(jobs);
    }
    
    
    [HttpGet("filter/salary")]
    public async Task<ActionResult<IEnumerable<Job>>> FilterBySalary(
        [FromQuery] decimal? minSalary,
        [FromQuery] decimal? maxSalary)
    {
        if (minSalary.HasValue && maxSalary.HasValue && minSalary > maxSalary)
        {
            return BadRequest("Min salary must be less than Max salary.");
        }

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
    
}