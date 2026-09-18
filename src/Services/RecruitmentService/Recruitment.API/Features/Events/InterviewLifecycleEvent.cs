using Recruitment.API.Entities;

namespace Recruitment.API.Features.Events;

public sealed record InterviewLifecycleEvent(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    Guid InterviewScheduleId,
    Guid SelectionRoundId,
    Guid CandidateProfileId,
    string? CandidateUserId,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc,
    string? GoogleMeetUrl,
    DateTimeOffset OccurredAtUtc)
{
    public const string ScheduledType = "interview.scheduled.v1";
    public const string RescheduledType = "interview.rescheduled.v1";
    public const string CancelledType = "interview.cancelled.v1";

    public static InterviewLifecycleEvent Scheduled(InterviewSchedule schedule, string? candidateUserId) =>
        Create(schedule, ScheduledType, candidateUserId);
    public static InterviewLifecycleEvent Rescheduled(InterviewSchedule schedule, string? candidateUserId) =>
        Create(schedule, RescheduledType, candidateUserId);
    public static InterviewLifecycleEvent Cancelled(InterviewSchedule schedule, string? candidateUserId) =>
        Create(schedule, CancelledType, candidateUserId);

    private static InterviewLifecycleEvent Create(InterviewSchedule schedule, string eventType, string? candidateUserId) => new(
        Guid.NewGuid(), eventType, 1, schedule.Id, schedule.SelectionRoundId, schedule.CandidateProfileId,
        candidateUserId,
        new DateTimeOffset(DateTime.SpecifyKind(schedule.StartTime, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(schedule.EndTime, DateTimeKind.Utc)),
        schedule.GoogleMeetUrl, DateTimeOffset.UtcNow);
}
