using Microsoft.EntityFrameworkCore;
using Notification.API.Data;
using Notification.API.Models;

namespace Notification.API.Messaging;

public sealed class LifecycleNotificationSink(NotificationDbContext db) : ILifecycleNotificationSink
{
    public async Task<bool> TryPersistAsync(LifecycleNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (await db.ProcessedBrokerEvents.AnyAsync(e => e.EventId == notification.EventId, cancellationToken))
        {
            return false;
        }

        db.Notifications.Add(new UserNotification
        {
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message
        });
        db.ProcessedBrokerEvents.Add(new ProcessedBrokerEvent
        {
            EventId = notification.EventId,
            EventType = notification.EventType,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Concurrent consumer already stored this EventId — treat as handled.
            return false;
        }
    }
}
