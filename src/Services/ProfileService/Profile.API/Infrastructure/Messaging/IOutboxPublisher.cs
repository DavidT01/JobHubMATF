using Profile.API.Data.Outbox;

namespace Profile.API.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
