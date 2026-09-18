using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.API.Messaging;

public sealed class RabbitNotificationConsumer(
    IConnectionFactory connectionFactory,
    RabbitMessagingOptions options,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitNotificationConsumer> logger) : BackgroundService
{
    private readonly RabbitMessagingOptions _options = options;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notification RabbitMQ consumer disconnected; retrying.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task RunSessionAsync(CancellationToken stoppingToken)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 10, false, stoppingToken);

        await BindQueueAsync(channel, _options.ApplicationsExchange, _options.ApplicationsQueue,
            ["application.submitted.v1", "application.status-changed.v1"], stoppingToken);
        await BindQueueAsync(channel, _options.RecruitmentExchange, _options.RecruitmentQueue,
            [
                "interview.scheduled.v1",
                "interview.rescheduled.v1",
                "interview.cancelled.v1",
                "candidate.round-advanced.v1",
                "candidate.rejected.v1",
                "candidate.hired.v1"
            ], stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                await HandleMessageAsync(args, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process broker event {RoutingKey}.", args.RoutingKey);
                await channel.BasicNackAsync(args.DeliveryTag, false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(_options.ApplicationsQueue, autoAck: false, consumer, stoppingToken);
        await channel.BasicConsumeAsync(_options.RecruitmentQueue, autoAck: false, consumer, stoppingToken);

        logger.LogInformation(
            "Notification RabbitMQ consumer listening on {ApplicationsQueue} and {RecruitmentQueue}.",
            _options.ApplicationsQueue, _options.RecruitmentQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs args, CancellationToken cancellationToken)
    {
        var mapped = LifecycleEventMapper.TryMap(args.RoutingKey, args.Body.Span);
        if (mapped is null)
        {
            logger.LogDebug(
                "Skipping broker message {RoutingKey} (unmapped or missing user id). BodyLength={Length}",
                args.RoutingKey, args.Body.Length);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var sink = scope.ServiceProvider.GetRequiredService<ILifecycleNotificationSink>();
        var created = await sink.TryPersistAsync(mapped, cancellationToken);
        if (created)
        {
            logger.LogInformation(
                "Stored notification from {EventType} for user {UserId}.",
                mapped.EventType, mapped.UserId);
        }
    }

    private static async Task BindQueueAsync(
        IChannel channel,
        string exchange,
        string queue,
        IReadOnlyList<string> routingKeys,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, autoDelete: false,
            arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: null, passive: false, noWait: false, cancellationToken: cancellationToken);
        foreach (var key in routingKeys)
        {
            await channel.QueueBindAsync(queue, exchange, key, arguments: null, noWait: false,
                cancellationToken: cancellationToken);
        }
    }
}
