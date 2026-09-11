using System.Text.Json;
using ApplicationService.Application.Authorization;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Commands;
using ApplicationService.Application.Events;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Handlers;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Recruitment;
using ApplicationService.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ApplicationService.UnitTests.Application;

public sealed class SubmissionOutboxTests
{
    private const string JobId = "507f1f77bcf86cd799439011";
    private static readonly Guid CandidateId = Guid.NewGuid();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Successful_submission_stores_one_event_even_when_recruitment_is_down(bool recruitmentDown)
    {
        await using var db = Database();
        var handler = Handler(db, recruitmentDown);
        var result = await handler.Handle(new SubmitApplicationCommand(JobId, "Private cover letter"), TestContext.Current.CancellationToken);
        var row = Assert.Single(await db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
        var application = Assert.Single(await db.JobApplications.ToListAsync(TestContext.Current.CancellationToken));
        var message = JsonSerializer.Deserialize<ApplicationLifecycleEvent>(row.Payload, JsonSerializerOptions.Web)!;
        Assert.Equal(result.Id, row.ApplicationId);
        Assert.Equal(application.Id, message.ApplicationId);
        Assert.Equal(CandidateId, message.CandidateProfileId);
        Assert.Equal(ApplicationLifecycleEvent.SubmittedType, row.EventType);
        Assert.Null(row.PublishedAtUtc);
        Assert.DoesNotContain("Private cover letter", row.Payload);
    }

    [Fact]
    public async Task Duplicate_submission_does_not_enqueue_another_event()
    {
        await using var db = Database();
        var handler = Handler(db);
        await handler.Handle(new SubmitApplicationCommand(JobId, null), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new SubmitApplicationCommand(JobId, null), TestContext.Current.CancellationToken));
        Assert.Single(await db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task One_save_contains_both_application_and_outbox_and_failure_does_not_call_recruitment()
    {
        var interceptor = new RejectSave();
        await using var db = Database(interceptor);
        await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db).Handle(new SubmitApplicationCommand(JobId, null), TestContext.Current.CancellationToken));
        Assert.Equal(1, interceptor.Saves);
        Assert.Empty(await db.JobApplications.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    private static ApplicationDbContext Database(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptors).Options);

    private static SubmitApplicationHandler Handler(ApplicationDbContext db, bool down = false) => new(
        db, new User(), new Profiles(), new Jobs(), TimeProvider.System,
        new Recruitment(db, down), NullLogger<SubmitApplicationHandler>.Instance);

    private sealed class User : ICurrentUser
    {
        public string UserId => "candidate-user";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == "Candidate";
    }
    private sealed class Profiles : ICandidateProfileReader
    {
        public Task<CandidateProfileReference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<CandidateProfileReference?>(new(CandidateId, userId, "/cv/current.pdf", null, null));
    }
    private sealed class Jobs : IJobReader
    {
        public Task<CatalogJob?> GetByIdAsync(string jobId, CancellationToken cancellationToken) =>
            Task.FromResult<CatalogJob?>(new(Guid.NewGuid(), true, null));
    }
    private sealed class Recruitment(ApplicationDbContext db, bool down) : IRecruitmentClient
    {
        public async Task ActivateCandidateProgressAsync(Guid candidateProfileId, string jobId, Guid applicationId, CancellationToken cancellationToken)
        {
            Assert.True(await db.JobApplications.AnyAsync(x => x.Id == applicationId, cancellationToken));
            Assert.True(await db.OutboxMessages.AnyAsync(x => x.ApplicationId == applicationId, cancellationToken));
            if (down) throw new DependencyUnavailableException("Recruitment");
        }
    }
    private sealed class RejectSave : SaveChangesInterceptor
    {
        public int Saves { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Saves++;
            Assert.Equal(2, eventData.Context!.ChangeTracker.Entries().Count(x => x.State == EntityState.Added));
            throw new DbUpdateException("Simulated save rejection");
        }
    }
}
