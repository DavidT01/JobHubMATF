using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Identity.API.Services;

public sealed class HttpNotificationPublisher(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<HttpNotificationPublisher> logger) : INotificationPublisher
{
    public async Task NotifyAsync(string userId, string title, string message)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/notifications");
            AddApiKey(request);
            request.Content = JsonContent.Create(new { userId, title, message });
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notification.API create failed with {StatusCode} for user {UserId}",
                    (int)response.StatusCode,
                    userId);
            }
        }
        catch (Exception ex)
        {
            // Registration/admin flows must not fail if Notification service is down.
            logger.LogWarning(ex, "Could not reach Notification.API for user {UserId}", userId);
        }
    }

    public async Task NotifyManyAsync(IEnumerable<string> userIds, string title, string message)
    {
        var ids = userIds.Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
        {
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/notifications/batch");
            AddApiKey(request);
            request.Content = JsonContent.Create(new { userIds = ids, title, message });
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notification.API batch create failed with {StatusCode}",
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not reach Notification.API for batch notify");
        }
    }

    private void AddApiKey(HttpRequestMessage request)
    {
        var apiKey = configuration["NotificationApi:ApiKey"]
            ?? throw new InvalidOperationException("NotificationApi:ApiKey is missing.");
        request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
    }
}
