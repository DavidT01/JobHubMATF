namespace Identity.API.Services;

/// <summary>
/// Publishes in-app notifications (local DB in tests, Notification.API in prod/dev).
/// </summary>
public interface INotificationPublisher
{
    Task NotifyAsync(string userId, string title, string message);
    Task NotifyManyAsync(IEnumerable<string> userIds, string title, string message);
}
