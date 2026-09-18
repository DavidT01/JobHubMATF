using Microsoft.EntityFrameworkCore;
using Notification.API.Data;
using Notification.API.Models;

namespace Notification.API.Services;

public class NotificationAppService(NotificationDbContext db) : INotificationService
{
    public async Task NotifyAsync(string userId, string title, string message)
    {
        db.Notifications.Add(new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message
        });
        await db.SaveChangesAsync();
    }

    public async Task NotifyManyAsync(IEnumerable<string> userIds, string title, string message)
    {
        var uniqueIds = userIds.Distinct(StringComparer.Ordinal).ToList();
        if (uniqueIds.Count == 0)
        {
            return;
        }

        foreach (var userId in uniqueIds)
        {
            db.Notifications.Add(new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message
            });
        }

        await db.SaveChangesAsync();
    }

    public Task<List<UserNotification>> ListForUserAsync(string userId) =>
        db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

    public Task<int> CountUnreadAsync(string userId) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> MarkReadAsync(string userId, Guid notificationId)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }

        return true;
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0)
        {
            return;
        }

        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync();
    }
}
