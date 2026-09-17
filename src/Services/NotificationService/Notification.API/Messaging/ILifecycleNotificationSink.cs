namespace Notification.API.Messaging;

public interface ILifecycleNotificationSink
{
    Task<bool> TryPersistAsync(LifecycleNotification notification, CancellationToken cancellationToken);
}
