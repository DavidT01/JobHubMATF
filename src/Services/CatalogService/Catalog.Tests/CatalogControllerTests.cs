using System.Text;
using System.Text.Json;
using Catalog.Authorization;
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
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly CatalogController controller;

    public CatalogControllerTests()
    {
        controller = new CatalogController(
            repository.Object,
            matchingService.Object,
            profileApiClient.Object,
            bookmarkRepository.Object,
            cache.Object,
            currentUser.Object);
    }

    private static Job CreateJob(string id = "job-1", string companyId = "company-1") => new()
    {
        Id = id,
        Title = "Backend Developer",
        CompanyId = companyId
    };

    private void SignedInAs(string userId) => currentUser.Setup(c => c.UserId).Returns(userId);

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
    public async Task CreateJob_invalidates_job_list_caches_when_employer_owns_the_company()
    {
        var job = CreateJob();
        SignedInAs("user-1");
        profileApiClient.Setup(p => p.GetCompanyProfileIdByUserIdAsync("user-1")).ReturnsAsync(job.CompanyId);
        repository.Setup(r => r.CreateJobAsync(job)).Returns(Task.CompletedTask);

        var result = await controller.CreateJob(job);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        cache.Verify(c => c.RemoveAsync("allJobs", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("activeJobs", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateJob_is_forbidden_when_job_belongs_to_a_different_company()
    {
        var job = CreateJob(companyId: "company-1");
        SignedInAs("user-1");
        profileApiClient.Setup(p => p.GetCompanyProfileIdByUserIdAsync("user-1")).ReturnsAsync("company-2");

        var result = await controller.CreateJob(job);

        Assert.IsType<ForbidResult>(result.Result);
        repository.Verify(r => r.CreateJobAsync(It.IsAny<Job>()), Times.Never);
    }

    [Fact]
    public async Task CreateJob_is_forbidden_when_caller_has_no_company_profile()
    {
        var job = CreateJob();
        SignedInAs("user-1");
        profileApiClient.Setup(p => p.GetCompanyProfileIdByUserIdAsync("user-1")).ReturnsAsync((string?)null);

        var result = await controller.CreateJob(job);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task DeleteJob_is_forbidden_when_caller_does_not_own_the_job()
    {
        var job = CreateJob(companyId: "company-1");
        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        SignedInAs("user-1");
        profileApiClient.Setup(p => p.GetCompanyProfileIdByUserIdAsync("user-1")).ReturnsAsync("company-2");

        var result = await controller.DeleteJob(job.Id);

        Assert.IsType<ForbidResult>(result);
        repository.Verify(r => r.DeleteJobAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteJob_succeeds_when_caller_owns_the_job()
    {
        var job = CreateJob(companyId: "company-1");
        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        SignedInAs("user-1");
        profileApiClient.Setup(p => p.GetCompanyProfileIdByUserIdAsync("user-1")).ReturnsAsync("company-1");
        repository.Setup(r => r.DeleteJobAsync(job.Id)).ReturnsAsync(true);

        var result = await controller.DeleteJob(job.Id);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task MatchJobToCandidate_returns_not_found_when_job_does_not_exist()
    {
        SignedInAs("user-1");
        repository.Setup(r => r.GetByIdAsync("missing-job", It.IsAny<CancellationToken>())).ReturnsAsync((Job?)null);

        var result = await controller.MatchJobToCandidate("missing-job", "user-1");

        Assert.IsType<NotFoundObjectResult>(result);
        profileApiClient.Verify(p => p.GetCandidateByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MatchJobToCandidate_is_forbidden_for_a_different_users_data()
    {
        SignedInAs("user-1");

        var result = await controller.MatchJobToCandidate("job-1", "someone-else");

        Assert.IsType<ForbidResult>(result);
        repository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MatchJobToCandidate_returns_not_found_when_candidate_does_not_exist()
    {
        var job = CreateJob();
        SignedInAs("user-1");
        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        cache.Setup(c => c.GetAsync("candidate:user-1", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
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

        SignedInAs("user-1");
        repository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);
        cache.Setup(c => c.GetAsync("candidate:user-1", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        profileApiClient.Setup(p => p.GetCandidateByIdAsync("user-1")).ReturnsAsync(candidate);
        matchingService.Setup(m => m.CalculateMatch(job, candidate)).Returns(expected);

        var result = await controller.MatchJobToCandidate(job.Id, "user-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task AddBookmark_is_forbidden_when_userId_is_not_the_caller()
    {
        SignedInAs("user-1");

        var result = await controller.AddBookmark("someone-else", "job-1");

        Assert.IsType<ForbidResult>(result);
        bookmarkRepository.Verify(b => b.IsBookmarkedAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddBookmark_returns_conflict_when_already_bookmarked()
    {
        SignedInAs("user-1");
        bookmarkRepository.Setup(b => b.IsBookmarkedAsync("user-1", "job-1")).ReturnsAsync(true);

        var result = await controller.AddBookmark("user-1", "job-1");

        Assert.IsType<ConflictObjectResult>(result);
        bookmarkRepository.Verify(b => b.AddAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddBookmark_adds_and_returns_ok_when_not_already_bookmarked()
    {
        SignedInAs("user-1");
        bookmarkRepository.Setup(b => b.IsBookmarkedAsync("user-1", "job-1")).ReturnsAsync(false);

        var result = await controller.AddBookmark("user-1", "job-1");

        Assert.IsType<OkResult>(result);
        bookmarkRepository.Verify(b => b.AddAsync("user-1", "job-1"), Times.Once);
    }

    [Fact]
    public async Task RemoveBookmark_is_forbidden_when_userId_is_not_the_caller()
    {
        SignedInAs("user-1");

        var result = await controller.RemoveBookmark("someone-else", "job-1");

        Assert.IsType<ForbidResult>(result);
        bookmarkRepository.Verify(b => b.RemoveAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveBookmark_returns_not_found_when_bookmark_does_not_exist()
    {
        SignedInAs("user-1");
        bookmarkRepository.Setup(b => b.RemoveAsync("user-1", "job-1")).ReturnsAsync(false);

        var result = await controller.RemoveBookmark("user-1", "job-1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveBookmark_returns_ok_when_removed()
    {
        SignedInAs("user-1");
        bookmarkRepository.Setup(b => b.RemoveAsync("user-1", "job-1")).ReturnsAsync(true);

        var result = await controller.RemoveBookmark("user-1", "job-1");

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task GetBookmarkedJobs_is_forbidden_for_a_different_users_data()
    {
        SignedInAs("user-1");

        var result = await controller.GetBookmarkedJobs("someone-else");

        Assert.IsType<ForbidResult>(result.Result);
        bookmarkRepository.Verify(b => b.GetByUserIdAsync(It.IsAny<string>()), Times.Never);
    }
}
