using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Handlers;
using ApplicationService.Application.Queries;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.UnitTests.Application;

public sealed class ApplicationStatisticsHandlerTests
{
    private static readonly Guid CandidateId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string JobId = "0123456789abcdef01234567";

    [Fact]
    public async Task Statistics_include_counts_rates_and_daily_trend_for_the_requested_period()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var submitted = CreateApplication("first-user", "2026-09-01T08:00:00Z");
        var accepted = CreateApplication("second-user", "2026-09-02T08:00:00Z");
        accepted.ChangeStatus(ApplicationStatus.InReview, DateTimeOffset.Parse("2026-09-03T08:00:00Z"));
        accepted.ChangeStatus(ApplicationStatus.Accepted, DateTimeOffset.Parse("2026-09-04T08:00:00Z"));
        var rejected = CreateApplication("third-user", "2026-09-02T12:00:00Z");
        rejected.ChangeStatus(ApplicationStatus.Rejected, DateTimeOffset.Parse("2026-09-03T12:00:00Z"));
        var outsidePeriod = CreateApplication("old-user", "2026-08-31T08:00:00Z");
        dbContext.AddRange(submitted, accepted, rejected, outsidePeriod);
        await dbContext.SaveChangesAsync(cancellationToken);

        var handler = new GetApplicationStatisticsHandler(dbContext);
        var result = await handler.Handle(
            new GetApplicationStatisticsQuery(
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 2)),
            cancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, FindStatus(result, ApplicationStatus.Submitted).Count);
        Assert.Equal(0, FindStatus(result, ApplicationStatus.InReview).Count);
        Assert.Equal(1, FindStatus(result, ApplicationStatus.Accepted).Count);
        Assert.Equal(1, FindStatus(result, ApplicationStatus.Rejected).Count);
        Assert.Equal(33.33m, FindStatus(result, ApplicationStatus.Accepted).RatePercent);
        Assert.Equal(
            [new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2)],
            result.DailyTrend.Select(item => item.Date));
        Assert.Equal([1, 2], result.DailyTrend.Select(item => item.Count));
    }

    [Fact]
    public async Task Empty_statistics_return_every_status_with_zero_values()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApplicationStatisticsHandler(dbContext);

        var result = await handler.Handle(
            new GetApplicationStatisticsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(Enum.GetValues<ApplicationStatus>(), result.ByStatus.Select(item => item.Status));
        Assert.All(result.ByStatus, item =>
        {
            Assert.Equal(0, item.Count);
            Assert.Equal(0m, item.RatePercent);
        });
        Assert.Empty(result.DailyTrend);
    }

    [Fact]
    public async Task Statistics_reject_an_inverted_period()
    {
        await using var dbContext = CreateDbContext();
        var handler = new GetApplicationStatisticsHandler(dbContext);

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => handler.Handle(
            new GetApplicationStatisticsQuery(
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 1)),
            TestContext.Current.CancellationToken));

        Assert.Contains("from", exception.Errors.Keys);
    }

    private static ApplicationService.Application.DTOs.ApplicationStatusStatisticsDto FindStatus(
        ApplicationService.Application.DTOs.ApplicationStatisticsDto statistics,
        ApplicationStatus status) => statistics.ByStatus.Single(item => item.Status == status);

    private static JobApplication CreateApplication(string userId, string submittedAt) =>
        JobApplication.Create(CandidateId, userId, JobId, null, DateTimeOffset.Parse(submittedAt));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
