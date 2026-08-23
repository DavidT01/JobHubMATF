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
            var credentialPath = _configuration["Google:CredentialsPath"];
            if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
            {
                throw new Exception("Google calendar credentials not configured or file not found.");
            }

            var credential = GoogleCredential.FromFile(credentialPath).CreateScoped(CalendarService.ScopeConstants.CalendarEvents);

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "JobHub"
            });

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
    }
}
