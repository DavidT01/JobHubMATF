using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace Recruitment.API.Infrastructure
{
    public class GoogleMeetingService(IConfiguration configuration) : IMeetingService
    {
        private readonly IConfiguration _configuration = configuration;

        public async Task<(string EventId, string MeetUrl)> ScheduleMeetingAsync(string title, string description, DateTime start, DateTime end, string[] attendeeEmails)
        {
            var service = CreateCalendarService();

            var newEvent = new Event()
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime() { DateTimeDateTimeOffset = start },
                End = new EventDateTime() { DateTimeDateTimeOffset = end },
                Attendees = [.. attendeeEmails.Select(email => new EventAttendee() { Email = email })],
                ConferenceData = new ConferenceData
                {
                    CreateRequest = new CreateConferenceRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" }
                    }
                }
            };

            var request = service.Events.Insert(newEvent, "primary");
            request.ConferenceDataVersion = 1;

            var createdEvent = await request.ExecuteAsync();

            var meetUrl = createdEvent.ConferenceData?.EntryPoints?.FirstOrDefault(e => e.EntryPointType == "video")?.Uri;

            return (createdEvent.Id, meetUrl ?? string.Empty);
        }

        public async Task UpdateMeetingAsync(string eventId, string title, string description, DateTime start, DateTime end, string[] attendeeEmails)
        {
            var service = CreateCalendarService();
            var existingEvent = await service.Events.Get("primary", eventId).ExecuteAsync();
            existingEvent.Summary = title;
            existingEvent.Description = description;
            existingEvent.Start = new EventDateTime { DateTimeDateTimeOffset = start };
            existingEvent.End = new EventDateTime { DateTimeDateTimeOffset = end };
            existingEvent.Attendees = [.. attendeeEmails.Select(email => new EventAttendee { Email = email })];

            await service.Events.Update(existingEvent, "primary", eventId).ExecuteAsync();
        }

        public async Task DeleteMeetingAsync(string eventId)
        {
            var service = CreateCalendarService();
            await service.Events.Delete("primary", eventId).ExecuteAsync();
        }

        private CalendarService CreateCalendarService()
        {
            var credentialPath = _configuration["Google:CredentialsPath"];
            if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
            {
                throw new InvalidOperationException("Google calendar credentials are not configured or the file was not found.");
            }

            var credential = GoogleCredential.FromFile(credentialPath).CreateScoped(CalendarService.ScopeConstants.CalendarEvents);
            return new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "JobHub"
            });
        }
    }
}
