using System.Text;
using System.Text.Json;
using Catalog.Clients;
using Catalog.Controllers;
using Catalog.DTOs;
using Catalog.Entities;
using Catalog.Repositories;
using Catalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace Catalog.Tests;

public sealed class CatalogControllerTests
{
    private readonly Mock<IJobRepository> repository = new();
    private readonly Mock<IMatchingService> matchingService = new();
    private readonly Mock<IProfileApiClient> profileApiClient = new();
    private readonly Mock<IBookmarkRepository> bookmarkRepository = new();
    private readonly Mock<IDistributedCache> cache = new();
    private readonly CatalogController controller;

    public CatalogControllerTests()
    {
        controller = new CatalogController(
            repository.Object,
            matchingService.Object,
            profileApiClient.Object,
            bookmarkRepository.Object,
            cache.Object);
    }

    private static Job CreateJob(string id = "job-1") => new() { Id = id, Title = "Backend Developer" };

    [Fact]
    public async Task GetAllJobs_returns_cached_value_without_calling_repository()
    {
        var cachedJobs = new List<Job> { CreateJob() };
        cache.Setup(c => c.GetAsync("allJobs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cachedJobs)));

        var result = await controller.GetAllJobs();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var jobs = Assert.IsAssignableFrom<IEnumerable<Job>>(ok.Value);
        Assert.Single(jobs);
        repository.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllJobs_falls_back_to_repository_on_cache_miss_and_populates_cache()
    {
        cache.Setup(c => c.GetAsync("allJobs", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        repository.Setup(r => r.GetAllAsync()).ReturnsAsync([CreateJob()]);

        var result = await controller.GetAllJobs();

        Assert.IsType<OkObjectResult>(result.Result);
        repository.Verify(r => r.GetAllAsync(), Times.Once);
        cache.Verify(c => c.SetAsync("allJobs", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJob_invalidates_job_list_caches()
    {
        var job = CreateJob();
        repository.Setup(r => r.CreateJobAsync(job)).Returns(Task.CompletedTask);

        await controller.CreateJob(job);

        cache.Verify(c => c.RemoveAsync("allJobs", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("activeJobs", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MatchJobToCandidate_returns_not_found_when_job_does_not_exist()
    {
        repository.Setup(r => r.GetByIdAsync("missing-job", It.IsAny<CancellationToken>())).ReturnsAsync((Job?)null);

        var result = await controller.MatchJobToCandidate("missing-job", "user-1");

        Assert.IsType<NotFoundObjectResult>(result);
        profileApiClient.Verify(p => p.GetCandidateByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MatchJobToCandidate_returns_not_found_when_candidate_does_not_exist()
    {
        var job = CreateJob();
        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        cache.Setup(c => c.GetAsync($"candidate:user-1", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        profileApiClient.Setup(p => p.GetCandidateByIdAsync("user-1")).ReturnsAsync((CandidateProfileDto?)null);

        var result = await controller.MatchJobToCandidate(job.Id, "user-1");

        Assert.IsType<NotFoundObjectResult>(result);
        matchingService.Verify(m => m.CalculateMatch(It.IsAny<Job>(), It.IsAny<CandidateProfileDto>()), Times.Never);
    }

    [Fact]
    public async Task MatchJobToCandidate_returns_match_result_on_success()
    {
        var job = CreateJob();
        var candidate = new CandidateProfileDto { Id = "profile-1" };
        var expected = new MatchResultDto { JobId = job.Id, Score = 87.5 };

        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        cache.Setup(c => c.GetAsync("candidate:user-1", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        profileApiClient.Setup(p => p.GetCandidateByIdAsync("user-1")).ReturnsAsync(candidate);
        matchingService.Setup(m => m.CalculateMatch(job, candidate)).Returns(expected);

        var result = await controller.MatchJobToCandidate(job.Id, "user-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task AddBookmark_returns_conflict_when_already_bookmarked()
    {
        bookmarkRepository.Setup(b => b.IsBookmarkedAsync("user-1", "job-1")).ReturnsAsync(true);

        var result = await controller.AddBookmark("user-1", "job-1");

        Assert.IsType<ConflictObjectResult>(result);
        bookmarkRepository.Verify(b => b.AddAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddBookmark_adds_and_returns_ok_when_not_already_bookmarked()
    {
        bookmarkRepository.Setup(b => b.IsBookmarkedAsync("user-1", "job-1")).ReturnsAsync(false);

        var result = await controller.AddBookmark("user-1", "job-1");

        Assert.IsType<OkResult>(result);
        bookmarkRepository.Verify(b => b.AddAsync("user-1", "job-1"), Times.Once);
    }

    [Fact]
    public async Task RemoveBookmark_returns_not_found_when_bookmark_does_not_exist()
    {
        bookmarkRepository.Setup(b => b.RemoveAsync("user-1", "job-1")).ReturnsAsync(false);

        var result = await controller.RemoveBookmark("user-1", "job-1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveBookmark_returns_ok_when_removed()
    {
        bookmarkRepository.Setup(b => b.RemoveAsync("user-1", "job-1")).ReturnsAsync(true);

        var result = await controller.RemoveBookmark("user-1", "job-1");

        Assert.IsType<OkResult>(result);
    }
}
