namespace Recruitment.API.Infrastructure
{
    public interface IMeetingService
    {
        Task<(string EventId, string MeetUrl)> ScheduleMeetingAsync(string title, string description, DateTime start, DateTime end, string[] attendeeEmails);
    }
}
