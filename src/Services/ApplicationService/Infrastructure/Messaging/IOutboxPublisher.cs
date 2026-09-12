using ApplicationService.Persistence.Outbox;

namespace ApplicationService.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
