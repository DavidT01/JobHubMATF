using System.Text.Json;
using ApplicationService.Application.Events;
using ApplicationService.Domain.Entities;
using ApplicationService.Domain.Enums;
using Xunit;

namespace ApplicationService.UnitTests.Application;

public sealed class ApplicationLifecycleEventTests
{
    private static JobApplication Application() => JobApplication.Create(Guid.NewGuid(), "candidate-identity",
        "507F1F77BCF86CD799439011", "Private cover letter", new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.FromHours(2)));

    [Fact]
    public void Submitted_payload_preserves_references_and_business_time()
    {
        var application = Application();
        var message = ApplicationLifecycleEvent.Submitted(application);
        Assert.NotEqual(Guid.Empty, message.EventId);
        Assert.Equal(ApplicationLifecycleEvent.SubmittedType, message.EventType);
        Assert.Equal(1, message.SchemaVersion);
        Assert.Equal(application.Id, message.ApplicationId);
        Assert.Equal(application.CandidateId, message.CandidateProfileId);
        Assert.Equal("candidate-identity", message.CandidateUserId);
        Assert.Equal("507f1f77bcf86cd799439011", message.JobId);
        Assert.Equal("Submitted", message.Status);
        Assert.Null(message.PreviousStatus);
        Assert.Equal(application.SubmittedAtUtc, message.OccurredAtUtc);
        Assert.Equal(TimeSpan.Zero, message.OccurredAtUtc.Offset);
    }

    [Fact]
    public void Status_event_uses_change_time_and_old_and_new_status()
    {
        var application = Application();
        var changedAt = application.SubmittedAtUtc.AddMinutes(5);
        application.ChangeStatus(ApplicationStatus.InReview, changedAt);
        var message = ApplicationLifecycleEvent.StatusChanged(application, ApplicationStatus.Submitted);
        Assert.Equal(ApplicationLifecycleEvent.StatusChangedType, message.EventType);
        Assert.Equal("Submitted", message.PreviousStatus);
        Assert.Equal("InReview", message.Status);
        Assert.Equal(changedAt, message.OccurredAtUtc);
    }

    [Fact]
    public void Accepted_is_a_status_change_with_its_own_event_id()
    {
        var application = Application();
        var submitted = ApplicationLifecycleEvent.Submitted(application);
        application.ChangeStatus(ApplicationStatus.InReview, application.SubmittedAtUtc.AddMinutes(1));
        application.ChangeStatus(ApplicationStatus.Accepted, application.SubmittedAtUtc.AddMinutes(2));
        var accepted = ApplicationLifecycleEvent.StatusChanged(application, ApplicationStatus.InReview);
        Assert.Equal("Accepted", accepted.Status);
        Assert.NotEqual(submitted.EventId, accepted.EventId);
    }

    [Fact]
    public void Repeated_status_does_not_produce_an_event() => Assert.Throws<ArgumentException>(() =>
        ApplicationLifecycleEvent.StatusChanged(Application(), ApplicationStatus.Submitted));

    [Fact]
    public void Invalid_previous_status_is_rejected() => Assert.Throws<ArgumentException>(() =>
        ApplicationLifecycleEvent.StatusChanged(Application(), (ApplicationStatus)999));

    [Fact]
    public void Cannot_label_a_changed_application_as_newly_submitted()
    {
        var application = Application();
        application.ChangeStatus(ApplicationStatus.Rejected, application.SubmittedAtUtc.AddMinutes(1));
        Assert.Throws<ArgumentException>(() => ApplicationLifecycleEvent.Submitted(application));
    }

    [Fact]
    public void Stored_json_roundtrip_preserves_event_id_without_private_content()
    {
        var message = ApplicationLifecycleEvent.Submitted(Application());
        var json = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
        Assert.DoesNotContain("Private cover letter", json);
        Assert.DoesNotContain("cv", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(message, JsonSerializer.Deserialize<ApplicationLifecycleEvent>(json, JsonSerializerOptions.Web));
    }
}
