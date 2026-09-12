using System.Text.Json;
using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Persistence.Data;
using ApplicationService.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ApplicationService.UnitTests.Application;

public sealed class OutboxMessageTests
{
    private static ApplicationLifecycleEvent Event() => ApplicationLifecycleEvent.Submitted(JobApplication.Create(
        Guid.NewGuid(), "candidate", "507f1f77bcf86cd799439011", null, DateTimeOffset.UtcNow));

    [Fact]
    public void New_message_has_original_event_identity_and_unpublished_state()
    {
        var message = Event();
        var row = OutboxMessage.Create(message);
        Assert.Equal(message.EventId, row.Id);
        Assert.Equal(message.ApplicationId, row.ApplicationId);
        Assert.Equal(message.EventType, row.EventType);
        Assert.Equal(message, JsonSerializer.Deserialize<ApplicationLifecycleEvent>(row.Payload, JsonSerializerOptions.Web));
        Assert.Null(row.PublishedAtUtc);
        Assert.Equal(0, row.Attempts);
        Assert.Equal(message.OccurredAtUtc, row.NextAttemptAtUtc);
    }

    [Fact]
    public void Retry_changes_schedule_not_payload_or_event_id()
    {
        var message = Event();
        var row = OutboxMessage.Create(message);
        var payload = row.Payload;
        row.RecordFailure(message.OccurredAtUtc.AddMinutes(1));
        row.RecordFailure(message.OccurredAtUtc.AddMinutes(2));
        Assert.Equal(2, row.Attempts);
        Assert.Equal(payload, row.Payload);
        Assert.Equal(message.EventId, row.Id);
        Assert.Equal(message.OccurredAtUtc, row.OccurredAtUtc);
        Assert.Equal(message.OccurredAtUtc.AddMinutes(2), row.NextAttemptAtUtc);
    }

    [Fact]
    public void Confirmed_publish_is_idempotent_and_cannot_be_retried()
    {
        var row = OutboxMessage.Create(Event());
        var published = row.OccurredAtUtc.AddSeconds(1);
        row.MarkPublished(published);
        row.MarkPublished(published.AddMinutes(1));
        Assert.Equal(published, row.PublishedAtUtc);
        Assert.Throws<InvalidOperationException>(() => row.RecordFailure(published.AddMinutes(2)));
    }

    [Fact]
    public void Timestamps_cannot_move_backwards()
    {
        var row = OutboxMessage.Create(Event());
        Assert.Throws<ArgumentOutOfRangeException>(() => row.RecordFailure(row.OccurredAtUtc.AddSeconds(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => row.MarkPublished(row.OccurredAtUtc.AddSeconds(-1)));
    }

    [Fact]
    public void Unsupported_or_empty_event_identity_is_rejected()
    {
        var message = Event();
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(message with { SchemaVersion = 2 }));
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(message with { EventId = Guid.Empty }));
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(message with { EventType = "unknown" }));
    }

    [Fact]
    public void PostgreSql_model_maps_json_and_pending_index_without_database_access()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);
        var entity = context.Model.FindEntityType(typeof(OutboxMessage))!;
        Assert.Equal("application_outbox", entity.GetTableName());
        Assert.Equal("jsonb", entity.FindProperty(nameof(OutboxMessage.Payload))!.GetColumnType());
        Assert.Contains(entity.GetIndexes(), index => index.GetFilter() == "published_at_utc IS NULL");
        Assert.Equal(nameof(OutboxMessage.Id), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
    }
}
