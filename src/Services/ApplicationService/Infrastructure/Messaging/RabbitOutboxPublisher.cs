using System.Text;
using ApplicationService.Persistence.Outbox;
using RabbitMQ.Client;

namespace ApplicationService.Infrastructure.Messaging;

public sealed class RabbitOutboxPublisher : IOutboxPublisher, IAsyncDisposable
{
    private readonly IChannel channel;
    private readonly string exchange;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;

    private RabbitOutboxPublisher(IChannel channel, string exchange)
    {
        this.channel = channel;
        this.exchange = exchange;
    }

    // The caller owns/reuses the connection; this publisher owns one confirm channel.
    public static async Task<RabbitOutboxPublisher> CreateAsync(IConnection connection, string exchange, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(exchange) || Encoding.UTF8.GetByteCount(exchange) > 255 || exchange.StartsWith("amq.", StringComparison.Ordinal))
            throw new ArgumentException("A non-reserved exchange name of at most 255 UTF-8 bytes is required.", nameof(exchange));
        var channel = await connection.CreateChannelAsync(new CreateChannelOptions(
            publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), token);
        try
        {
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false,
                arguments: null, passive: false, noWait: false, cancellationToken: token);
            return new RabbitOutboxPublisher(channel, exchange);
        }
        catch
        {
            await channel.DisposeAsync();
            throw;
        }
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var acquired = false;
        try
        {
            await gate.WaitAsync(deadline.Token);
            acquired = true;
            ObjectDisposedException.ThrowIf(disposed, this);
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                MessageId = message.Id.ToString(),
                CorrelationId = message.ApplicationId.ToString(),
                Type = message.EventType,
                Timestamp = new AmqpTimestamp(message.OccurredAtUtc.ToUnixTimeSeconds())
            };
            // Tracking makes this await fail on nack or mandatory return, not just write to a socket.
            await channel.BasicPublishAsync(exchange, message.EventType, mandatory: true, properties,
                Encoding.UTF8.GetBytes(message.Payload), deadline.Token);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("RabbitMQ publish confirmation timed out.", exception);
        }
        finally { if (acquired) gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed) return;
            disposed = true;
            await channel.DisposeAsync();
        }
        finally { gate.Release(); }
    }
}
