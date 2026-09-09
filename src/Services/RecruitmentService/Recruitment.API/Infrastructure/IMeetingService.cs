namespace Recruitment.API.Infrastructure
{
    public interface IMeetingService
    {
        Task<(string EventId, string MeetUrl)> ScheduleMeetingAsync(string title, string description, DateTime start, DateTime end, string[] attendeeEmails);
        Task UpdateMeetingAsync(string eventId, string title, string description, DateTime start, DateTime end, string[] attendeeEmails);
        Task DeleteMeetingAsync(string eventId);
    }
}
