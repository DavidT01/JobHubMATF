using Notification.API.Models;

namespace Notification.API.Services;

public interface INotificationService
{
    Task NotifyAsync(string userId, string title, string message);
    Task NotifyManyAsync(IEnumerable<string> userIds, string title, string message);
    Task<List<UserNotification>> ListForUserAsync(string userId);
    Task<int> CountUnreadAsync(string userId);
    Task<bool> MarkReadAsync(string userId, Guid notificationId);
    Task MarkAllReadAsync(string userId);
}
