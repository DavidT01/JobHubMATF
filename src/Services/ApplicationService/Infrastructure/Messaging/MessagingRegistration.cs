namespace ApplicationService.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddOutboxMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Outbox").Get<OutboxMessagingOptions>() ?? new();
        if (!options.Enabled) return services;
        var factory = options.CreateConnectionFactory();
        services.AddSingleton(options);
        services.AddSingleton(factory);
        services.AddSingleton<IOutboxPublisher, RabbitPublisherConnection>();
        services.AddScoped<OutboxDelivery>();
        services.AddScoped<OutboxDispatcher>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
