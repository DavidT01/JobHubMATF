using ApplicationService.Application.Events;
using ApplicationService.Application.Authorization;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Handlers;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Recruitment;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using ApplicationService.Infrastructure.Messaging;
using ApplicationService.Persistence.Data;
using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

[Collection("PostgreSQL outbox integration")]
public sealed class PostgresOutboxTests
{
    [Fact]
    public async Task Postgres_outbox_preserves_order_locks_atomicity_and_recovery()
    {
        var connectionString = Environment.GetEnvironmentVariable("JOBHUB_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) Assert.Skip("Set JOBHUB_TEST_POSTGRES to an isolated PostgreSQL test database.");
        var schema = "outbox_test_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(connectionString);
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin))
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        var scopedConnection = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema }.ConnectionString;
        ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(scopedConnection, options => options.MigrationsHistoryTable("__EFMigrationsHistory", schema)).Options);
        try
        {
            var first = JobApplication.Create(Guid.NewGuid(), "candidate-one", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow.AddMinutes(-10));
            var firstEvent = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(first));
            first.ChangeStatus(ApplicationStatus.InReview, first.SubmittedAtUtc.AddMinutes(1));
            var secondEvent = OutboxMessage.Create(ApplicationLifecycleEvent.StatusChanged(first, ApplicationStatus.Submitted));
            var other = JobApplication.Create(Guid.NewGuid(), "candidate-two", "507f1f77bcf86cd799439012", null, DateTimeOffset.UtcNow.AddMinutes(-5));
            var otherEvent = OutboxMessage.Create(ApplicationLifecycleEvent.Submitted(other));
            await using (var db = Context())
            {
                await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
                db.JobApplications.AddRange(first, other);
                db.OutboxMessages.Add(firstEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.OutboxMessages.Add(secondEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.OutboxMessages.Add(otherEvent);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                Assert.True(firstEvent.Sequence < secondEvent.Sequence && secondEvent.Sequence < otherEvent.Sequence);
            }

            var publisher = new Publisher();
            async Task<OutboxDispatchResult> Dispatch()
            {
                await using var db = Context();
                return await new OutboxDispatcher(db, new OutboxDelivery(publisher, TimeProvider.System,
                    NullLogger<OutboxDelivery>.Instance), TimeProvider.System).DispatchOneAsync(TestContext.Current.CancellationToken);
            }
            publisher.Fail = true;
            Assert.Equal(OutboxDispatchResult.RetryScheduled, await Dispatch());
            publisher.Fail = false;
            Assert.Equal(OutboxDispatchResult.Published, await Dispatch());
            Assert.Equal(otherEvent.Id, publisher.Ids[^1]); // Failed predecessor blocks only its own application.
            Assert.Equal(OutboxDispatchResult.NoWork, await Dispatch());

            await using (var db = Context())
            {
                var row = await db.OutboxMessages.SingleAsync(x => x.Id == firstEvent.Id, TestContext.Current.CancellationToken);
                Assert.Equal(1, row.Attempts);
                Assert.Null(row.PublishedAtUtc);
                Assert.Equal(firstEvent.Id, row.Id);
                await db.OutboxMessages.Where(x => x.Id == firstEvent.Id).ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.NextAttemptAtUtc, DateTimeOffset.UtcNow.AddSeconds(-1)), TestContext.Current.CancellationToken);
            }

            publisher.Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            publisher.Release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var active = Dispatch();
            await publisher.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            try { Assert.Equal(OutboxDispatchResult.Busy, await Dispatch()); }
            finally { publisher.Release.TrySetResult(); }
            Assert.Equal(OutboxDispatchResult.Published, await active);
            publisher.Started = null;
            publisher.Release = null;
            Assert.Equal(OutboxDispatchResult.Published, await Dispatch());
            Assert.Equal(secondEvent.Id, publisher.Ids[^1]);
            Assert.Equal(OutboxDispatchResult.NoWork, await Dispatch());
            await using (var db = Context())
                Assert.Equal(3, await db.OutboxMessages.CountAsync(x => x.PublishedAtUtc != null, TestContext.Current.CancellationToken));

            // Fail the real INSERT after the handler has executed its status UPDATE.
            var ownership = new OwnershipSources();
            await using (var db = Context())
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE FUNCTION reject_outbox_write() RETURNS trigger LANGUAGE plpgsql AS $$
                    BEGIN RAISE EXCEPTION 'simulated storage failure'; END; $$;
                    CREATE TRIGGER reject_outbox BEFORE INSERT ON application_outbox
                    FOR EACH ROW EXECUTE FUNCTION reject_outbox_write();
                    """, TestContext.Current.CancellationToken);
                var handler = new ChangeApplicationStatusHandler(db, new JobOwnershipGuard(ownership, ownership, ownership),
                    TimeProvider.System, ownership, NullLogger<ChangeApplicationStatusHandler>.Instance);
                await Assert.ThrowsAsync<DbUpdateException>(() => handler.Handle(new(first.Id, ApplicationStatus.Accepted), TestContext.Current.CancellationToken));
                db.ChangeTracker.Clear();
                Assert.Equal(ApplicationStatus.InReview, (await db.JobApplications.SingleAsync(x => x.Id == first.Id, TestContext.Current.CancellationToken)).Status);
                Assert.Equal(3, await db.OutboxMessages.CountAsync(TestContext.Current.CancellationToken));
                Assert.Equal(0, ownership.RecruitmentCalls);
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_outbox ON application_outbox", TestContext.Current.CancellationToken);
                db.ChangeTracker.Clear();
                await handler.Handle(new(first.Id, ApplicationStatus.Accepted), TestContext.Current.CancellationToken);
                Assert.Equal(1, ownership.RecruitmentCalls);
                // Simulate a successful broker confirm followed by failed delivery-state persistence.
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TRIGGER reject_outbox_completion BEFORE UPDATE ON application_outbox
                    FOR EACH ROW EXECUTE FUNCTION reject_outbox_write();
                    """, TestContext.Current.CancellationToken);
            }
            await Assert.ThrowsAsync<DbUpdateException>(() => Dispatch());
            var confirmedId = publisher.Ids[^1];
            string originalPayload;
            await using (var db = Context())
            {
                var pending = await db.OutboxMessages.SingleAsync(x => x.Id == confirmedId, TestContext.Current.CancellationToken);
                Assert.Null(pending.PublishedAtUtc);
                originalPayload = pending.Payload;
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_outbox_completion ON application_outbox", TestContext.Current.CancellationToken);
            }
            Assert.Equal(OutboxDispatchResult.Published, await Dispatch()); // New DbContext, same retained event.
            Assert.Equal(confirmedId, publisher.Ids[^1]);
            Assert.Equal(2, publisher.Ids.Count(id => id == confirmedId));
            await using (var db = Context())
            {
                var delivered = await db.OutboxMessages.SingleAsync(x => x.Id == confirmedId, TestContext.Current.CancellationToken);
                Assert.Equal(originalPayload, delivered.Payload);
                Assert.NotNull(delivered.PublishedAtUtc);
            }
        }
        finally
        {
            // Exact schema created by this test; never drop the configured database.
            await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
            await drop.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }

    private sealed class OwnershipSources : ICurrentUser, ICompanyProfileReader, IJobReader, IRecruitmentClient
    {
        private readonly Guid companyId = Guid.NewGuid();
        public string UserId => "employer";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => role == "Employer";
        public int RecruitmentCalls { get; private set; }
        public Task<CompanyProfileReference?> GetByUserIdAsync(string id, CancellationToken ct) => Task.FromResult<CompanyProfileReference?>(new(companyId, id));
        public Task<CatalogJob?> GetByIdAsync(string id, CancellationToken ct) => Task.FromResult<CatalogJob?>(new(companyId, true, null));
        public Task ActivateCandidateProgressAsync(Guid profile, string job, Guid application, CancellationToken ct)
        {
            RecruitmentCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class Publisher : IOutboxPublisher
    {
        public bool Fail { get; set; }
        public List<Guid> Ids { get; } = [];
        public TaskCompletionSource? Started { get; set; }
        public TaskCompletionSource? Release { get; set; }
        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            Ids.Add(message.Id);
            if (Fail) throw new IOException("simulated broker failure");
            Started?.TrySetResult();
            if (Release is not null) await Release.Task.WaitAsync(cancellationToken);
        }
    }
}
