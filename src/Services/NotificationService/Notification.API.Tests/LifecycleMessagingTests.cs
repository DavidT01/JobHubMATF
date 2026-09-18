using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Notification.API.Data;
using Notification.API.Messaging;

namespace Notification.API.Tests;

public class LifecycleEventMapperTests
{
    [Fact]
    public void TryMap_ApplicationSubmitted_ReturnsNotification()
    {
        var eventId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId,
            eventType = "application.submitted.v1",
            candidateUserId = "user-1",
            jobId = "0123456789abcdef01234567",
            status = "Submitted"
        }));

        var mapped = LifecycleEventMapper.TryMap("application.submitted.v1", body);

        Assert.NotNull(mapped);
        Assert.Equal(eventId, mapped.EventId);
        Assert.Equal("user-1", mapped.UserId);
        Assert.Equal("Application submitted", mapped.Title);
        Assert.Contains("submitted", mapped.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_ApplicationStatusChanged_ReturnsStatusMessage()
    {
        var eventId = Guid.NewGuid();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId,
            candidateUserId = "user-2",
            jobId = "job-1",
            status = "Interview"
        }));

        var mapped = LifecycleEventMapper.TryMap("application.status-changed.v1", body);

        Assert.NotNull(mapped);
        Assert.Equal("Application status updated", mapped.Title);
        Assert.Contains("Interview", mapped.Message);
    }

    [Fact]
    public void TryMap_MissingCandidateUserId_ReturnsNull()
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId = Guid.NewGuid(),
            jobId = "job-1",
            status = "Submitted"
        }));

        Assert.Null(LifecycleEventMapper.TryMap("application.submitted.v1", body));
    }

    [Fact]
    public void TryMap_InterviewScheduled_ReturnsNotification()
    {
        var eventId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            eventId,
            candidateUserId = "user-3",
            startTimeUtc = start
        }));

        var mapped = LifecycleEventMapper.TryMap("interview.scheduled.v1", body);

        Assert.NotNull(mapped);
        Assert.Equal("Interview scheduled", mapped.Title);
        Assert.Equal("user-3", mapped.UserId);
    }

    [Fact]
    public void TryMap_UnknownRoutingKey_ReturnsNull()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.Null(LifecycleEventMapper.TryMap("something.else.v1", body));
    }
}

public class LifecycleNotificationSinkTests
{
    [Fact]
    public async Task TryPersistAsync_HappyPath_CreatesNotificationOnce()
    {
        await using var db = CreateDb();
        var sink = new LifecycleNotificationSink(db);
        var eventId = Guid.NewGuid();
        var notification = new LifecycleNotification(
            eventId, "application.submitted.v1", "user-a", "Title", "Message");

        Assert.True(await sink.TryPersistAsync(notification, CancellationToken.None));
        Assert.False(await sink.TryPersistAsync(notification, CancellationToken.None));

        Assert.Equal(1, await db.Notifications.CountAsync());
        Assert.Equal(1, await db.ProcessedBrokerEvents.CountAsync(e => e.EventId == eventId));
    }

    [Fact]
    public async Task TryPersistAsync_EmptyUser_StillRequiresValidPayloadFromMapper()
    {
        await using var db = CreateDb();
        var sink = new LifecycleNotificationSink(db);

        // Sink trusts mapped payload; mapper already rejects empty user.
        var notification = new LifecycleNotification(
            Guid.NewGuid(), "application.submitted.v1", "user-b", "T", "M");

        Assert.True(await sink.TryPersistAsync(notification, CancellationToken.None));
        Assert.Equal("user-b", (await db.Notifications.SingleAsync()).UserId);
    }

    private static NotificationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseSqlite($"Data Source=notif-sink-{Guid.NewGuid():N}.db")
            .Options;
        var db = new NotificationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
