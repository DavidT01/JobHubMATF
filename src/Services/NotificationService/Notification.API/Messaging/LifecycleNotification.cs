namespace Notification.API.Messaging;

public sealed record LifecycleNotification(Guid EventId, string EventType, string UserId, string Title, string Message);
