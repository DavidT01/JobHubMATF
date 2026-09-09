using ApplicationService.Application.Authorization;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Handlers;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Queries;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.UnitTests.Application;

public sealed class ApplicationQueryHandlerTests
{
    private static readonly Guid CandidateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string JobId = "0123456789abcdef01234567";

    [Fact]
    public async Task Candidate_query_filters_status_and_sorts_by_update_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var olderAccepted = CreateApplication("candidate-user", DateTimeOffset.Parse("2026-09-01T08:00:00Z"));
        olderAccepted.ChangeStatus(ApplicationStatus.InReview, DateTimeOffset.Parse("2026-09-03T08:00:00Z"));
        olderAccepted.ChangeStatus(ApplicationStatus.Accepted, DateTimeOffset.Parse("2026-09-05T08:00:00Z"));
        var newerAccepted = CreateApplication("candidate-user", DateTimeOffset.Parse("2026-09-02T08:00:00Z"));
        newerAccepted.ChangeStatus(ApplicationStatus.InReview, DateTimeOffset.Parse("2026-09-04T08:00:00Z"));
        newerAccepted.ChangeStatus(ApplicationStatus.Accepted, DateTimeOffset.Parse("2026-09-06T08:00:00Z"));
        var rejected = CreateApplication("candidate-user", DateTimeOffset.Parse("2026-09-03T08:00:00Z"));
        rejected.ChangeStatus(ApplicationStatus.Rejected, DateTimeOffset.Parse("2026-09-07T08:00:00Z"));
        var anotherCandidatesApplication = CreateApplication(
            "other-user",
            DateTimeOffset.Parse("2026-08-31T08:00:00Z"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"));
        anotherCandidatesApplication.ChangeStatus(
            ApplicationStatus.InReview, DateTimeOffset.Parse("2026-09-01T08:00:00Z"));
        anotherCandidatesApplication.ChangeStatus(
            ApplicationStatus.Accepted, DateTimeOffset.Parse("2026-09-02T08:00:00Z"));
        dbContext.AddRange(olderAccepted, newerAccepted, rejected, anotherCandidatesApplication);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handler = new GetCandidateApplicationsHandler(
            dbContext,
            new CurrentUser("candidate-user", "Candidate"),
            new CandidateReader(new CandidateProfileReference(CandidateId, "candidate-user", null, null, null)));

        var result = await handler.Handle(
            new GetCandidateApplicationsQuery(
                Status: ApplicationStatus.Accepted,
                SortBy: ApplicationSortBy.UpdatedAtUtc,
                SortDirection: SortDirection.Asc),
            cancellationToken);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([olderAccepted.Id, newerAccepted.Id], result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Candidate_query_rejects_sort_fields_outside_the_whitelist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var handler = new GetCandidateApplicationsHandler(
            dbContext,
            new CurrentUser("candidate-user", "Candidate"),
            new CandidateReader(new CandidateProfileReference(CandidateId, "candidate-user", null, null, null)));

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(
            new GetCandidateApplicationsQuery(SortBy: (ApplicationSortBy)999),
            cancellationToken));

        Assert.Contains("sortBy", exception.Errors.Keys);
    }

    [Fact]
    public async Task Employer_query_filters_status_and_sorts_by_submission_time()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var first = CreateApplication("first-user", DateTimeOffset.Parse("2026-09-01T08:00:00Z"));
        first.ChangeStatus(ApplicationStatus.Rejected, DateTimeOffset.Parse("2026-09-04T08:00:00Z"));
        var second = CreateApplication("second-user", DateTimeOffset.Parse("2026-09-02T08:00:00Z"));
        second.ChangeStatus(ApplicationStatus.Rejected, DateTimeOffset.Parse("2026-09-03T08:00:00Z"));
        var submitted = CreateApplication("third-user", DateTimeOffset.Parse("2026-09-03T08:00:00Z"));
        dbContext.AddRange(first, second, submitted);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handler = new GetEmployerApplicationsHandler(
            dbContext,
            CreateOwnershipGuard(),
            new CandidateReader(null),
            new CvLinkResolver());

        var result = await handler.Handle(
            new GetEmployerApplicationsQuery(
                JobId,
                Status: ApplicationStatus.Rejected,
                SortBy: ApplicationSortBy.SubmittedAtUtc,
                SortDirection: SortDirection.Asc),
            cancellationToken);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([first.Id, second.Id], result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Employer_query_rejects_sort_directions_outside_the_whitelist()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var handler = new GetEmployerApplicationsHandler(
            dbContext,
            CreateOwnershipGuard(),
            new CandidateReader(null),
            new CvLinkResolver());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(
            new GetEmployerApplicationsQuery(JobId, SortDirection: (SortDirection)999),
            cancellationToken));

        Assert.Contains("sortDirection", exception.Errors.Keys);
    }

    private static JobApplication CreateApplication(
        string userId, DateTimeOffset submittedAt, Guid? candidateId = null) =>
        JobApplication.Create(candidateId ?? CandidateId, userId, JobId, null, submittedAt);

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static JobOwnershipGuard CreateOwnershipGuard() => new(
        new CurrentUser("employer-user", "Employer"),
        new CompanyReader(new CompanyProfileReference(CompanyId, "employer-user")),
        new JobReader(new CatalogJob(CompanyId, true, null)));

    private sealed class CurrentUser(string userId, string role) : ICurrentUser
    {
        public string? UserId => userId;
        public bool IsAuthenticated => true;
        public bool IsInRole(string requestedRole) => requestedRole == role;
    }

    private sealed class CandidateReader(CandidateProfileReference? profile) : ICandidateProfileReader
    {
        public Task<CandidateProfileReference?> GetByUserIdAsync(
            string userId, CancellationToken cancellationToken) => Task.FromResult(profile);
    }

    private sealed class CompanyReader(CompanyProfileReference company) : ICompanyProfileReader
    {
        public Task<CompanyProfileReference?> GetByUserIdAsync(
            string userId, CancellationToken cancellationToken) => Task.FromResult<CompanyProfileReference?>(company);
    }

    private sealed class JobReader(CatalogJob job) : IJobReader
    {
        public Task<CatalogJob?> GetByIdAsync(
            string jobId, CancellationToken cancellationToken) => Task.FromResult<CatalogJob?>(job);
    }

    private sealed class CvLinkResolver : ICvLinkResolver
    {
        public string Resolve(string cvPath) => cvPath;
    }
}
