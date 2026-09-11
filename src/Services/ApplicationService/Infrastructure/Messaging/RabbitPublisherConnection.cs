using ApplicationService.Persistence.Outbox;
using RabbitMQ.Client;

namespace ApplicationService.Infrastructure.Messaging;

public sealed class RabbitPublisherConnection(IConnectionFactory factory, OutboxMessagingOptions options,
    ILogger<RabbitPublisherConnection> logger) : IOutboxPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;
    private RabbitOutboxPublisher? publisher;
    private bool disposed;

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await gate.WaitAsync(timeout.Token);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (connection is null || !connection.IsOpen || publisher is null)
            {
                await ResetAsync();
                connection = await factory.CreateConnectionAsync(timeout.Token);
                publisher = await RabbitOutboxPublisher.CreateAsync(connection, options.Exchange, timeout.Token);
            }
            await publisher.PublishAsync(message, timeout.Token);
        }
        catch
        {
            await ResetAsync();
            throw;
        }
        finally { gate.Release(); }
    }

    private async Task ResetAsync()
    {
        var oldPublisher = publisher;
        var oldConnection = connection;
        publisher = null;
        connection = null;
        try { if (oldPublisher is not null) await oldPublisher.DisposeAsync(); }
        catch (Exception ex) { logger.LogWarning("Publisher cleanup failed ({FailureType}).", ex.GetType().Name); }
        try { if (oldConnection is not null) await oldConnection.DisposeAsync(); }
        catch (Exception ex) { logger.LogWarning("Connection cleanup failed ({FailureType}).", ex.GetType().Name); }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            disposed = true;
            await ResetAsync();
        }
        finally { gate.Release(); }
    }
}
