using Recruitment.API.Data.Outbox;

namespace Recruitment.API.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}
