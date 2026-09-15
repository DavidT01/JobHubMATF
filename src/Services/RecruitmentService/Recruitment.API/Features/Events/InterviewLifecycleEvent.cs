using Recruitment.API.Entities;

namespace Recruitment.API.Features.Events;

public sealed record InterviewLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid InterviewScheduleId,
    Guid SelectionRoundId,
    Guid CandidateProfileId,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc,
    string? GoogleMeetUrl,
    DateTimeOffset OccurredAtUtc)
{
    public const string ScheduledType = "interview.scheduled.v1";
    public const string RescheduledType = "interview.rescheduled.v1";
    public const string CancelledType = "interview.cancelled.v1";

    public static InterviewLifecycleEvent Scheduled(InterviewSchedule schedule) => Create(schedule, ScheduledType);
    public static InterviewLifecycleEvent Rescheduled(InterviewSchedule schedule) => Create(schedule, RescheduledType);
    public static InterviewLifecycleEvent Cancelled(InterviewSchedule schedule) => Create(schedule, CancelledType);

    private static InterviewLifecycleEvent Create(InterviewSchedule schedule, string eventType) => new(
        Guid.NewGuid(), eventType, 1, schedule.Id, schedule.SelectionRoundId, schedule.CandidateProfileId,
        new DateTimeOffset(DateTime.SpecifyKind(schedule.StartTime, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(schedule.EndTime, DateTimeKind.Utc)),
        schedule.GoogleMeetUrl, DateTimeOffset.UtcNow);
}
