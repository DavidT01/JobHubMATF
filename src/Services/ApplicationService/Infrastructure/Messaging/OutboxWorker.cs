namespace ApplicationService.Infrastructure.Messaging;

public sealed class OutboxWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = TimeSpan.FromSeconds(2);
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var result = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchOneAsync(stoppingToken);
                if (result == OutboxDispatchResult.Published) wait = TimeSpan.Zero;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError("Outbox iteration failed ({FailureType}); stored events remain pending.", exception.GetType().Name);
                wait = TimeSpan.FromSeconds(5);
            }
            try { await Task.Delay(wait, clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
