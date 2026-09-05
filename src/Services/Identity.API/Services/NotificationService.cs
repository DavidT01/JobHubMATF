using Identity.API.Data;
using Identity.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Services;

public class NotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task NotifyAsync(string userId, string title, string message)
    {
        _db.Notifications.Add(new UserNotification
        {
            UserId = userId,
            Title = title,
            Message = message
        });
        await _db.SaveChangesAsync();
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
            _db.Notifications.Add(new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message
            });
        }

        await _db.SaveChangesAsync();
    }

    public Task<List<UserNotification>> ListForUserAsync(string userId) =>
        _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

    public Task<int> CountUnreadAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> MarkReadAsync(string userId, Guid notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return true;
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
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

        await _db.SaveChangesAsync();
    }
}
