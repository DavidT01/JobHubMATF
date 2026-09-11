using ApplicationService.Application.Authorization;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Commands;
using ApplicationService.Application.Handlers;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Recruitment;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Persistence.Data;
using ApplicationService.Persistence.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApplicationService.UnitTests.Application;

public sealed class StatusOutboxTransactionTests
{
    [Fact]
    public async Task Status_and_event_commit_together_and_repeat_is_noop()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = Database(connection);
        var application = await Seed(db);
        var recruitment = new Recruitment();
        var handler = Handler(db, recruitment);
        await handler.Handle(new(application.Id, ApplicationStatus.InReview), TestContext.Current.CancellationToken);
        await handler.Handle(new(application.Id, ApplicationStatus.InReview), TestContext.Current.CancellationToken);
        Assert.Equal(ApplicationStatus.InReview, (await db.JobApplications.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Single(await db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, recruitment.Calls);
    }

    [Fact]
    public async Task Outbox_insert_failure_rolls_back_the_executed_status_update()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = Database(connection);
        var application = await Seed(db);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_outbox BEFORE INSERT ON application_outbox BEGIN SELECT RAISE(ABORT, 'simulated outbox failure'); END;", TestContext.Current.CancellationToken);
        var recruitment = new Recruitment();
        await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db, recruitment)
            .Handle(new(application.Id, ApplicationStatus.InReview), TestContext.Current.CancellationToken));
        db.ChangeTracker.Clear();
        var stored = await db.JobApplications.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ApplicationStatus.Submitted, stored.Status);
        Assert.Equal(application.UpdatedAtUtc, stored.UpdatedAtUtc);
        Assert.Empty(await db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, recruitment.Calls);
    }

    [Fact]
    public async Task Accepted_repeat_does_not_repeat_recruitment_or_event()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = Database(connection);
        var application = await Seed(db, true);
        var recruitment = new Recruitment();
        var handler = Handler(db, recruitment);
        await handler.Handle(new(application.Id, ApplicationStatus.Accepted), TestContext.Current.CancellationToken);
        await handler.Handle(new(application.Id, ApplicationStatus.Accepted), TestContext.Current.CancellationToken);
        Assert.Equal(1, recruitment.Calls);
        Assert.Single(await db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static ApplicationDbContext Database(SqliteConnection connection) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>().Options);

    private static async Task<JobApplication> Seed(ApplicationDbContext db, bool inReview = false)
    {
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var application = JobApplication.Create(Guid.NewGuid(), "candidate", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow.AddHours(-1));
        if (inReview) application.ChangeStatus(ApplicationStatus.InReview, application.SubmittedAtUtc.AddMinutes(1));
        db.JobApplications.Add(application);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        return application;
    }

    private static ChangeApplicationStatusHandler Handler(ApplicationDbContext db, Recruitment recruitment) => new(
        db, new JobOwnershipGuard(new User(), new Company(), new Jobs()), TimeProvider.System,
        recruitment, NullLogger<ChangeApplicationStatusHandler>.Instance);
    private static readonly Guid CompanyId = Guid.NewGuid();
    private sealed class User : ICurrentUser
    {
        public string UserId => "employer";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == "Employer";
    }
    private sealed class Company : ICompanyProfileReader
    {
        public Task<CompanyProfileReference?> GetByUserIdAsync(string id, CancellationToken ct) => Task.FromResult<CompanyProfileReference?>(new(CompanyId, id));
    }
    private sealed class Jobs : IJobReader
    {
        public Task<CatalogJob?> GetByIdAsync(string id, CancellationToken ct) => Task.FromResult<CatalogJob?>(new(CompanyId, true, null));
    }
    private sealed class Recruitment : IRecruitmentClient
    {
        public int Calls { get; private set; }
        public Task ActivateCandidateProgressAsync(Guid profileId, string jobId, Guid applicationId, CancellationToken ct)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    // SQLite verifies relational transaction boundaries while PostgreSQL is unavailable.
    // These test-only mappings do not test PostgreSQL JSONB/identity or concurrency.
    public sealed class SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);
            modelBuilder.Entity<JobApplication>().Property(x => x.SubmittedAtUtc).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
            modelBuilder.Entity<JobApplication>().Property(x => x.UpdatedAtUtc).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
            modelBuilder.Entity<OutboxMessage>().Property(x => x.Sequence).ValueGeneratedNever();
        }
    }
}
