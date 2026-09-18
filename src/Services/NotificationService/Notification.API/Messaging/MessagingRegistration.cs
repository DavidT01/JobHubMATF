namespace Notification.API.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddNotificationMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMessagingOptions.SectionName).Get<RabbitMessagingOptions>()
            ?? new RabbitMessagingOptions();

        services.AddScoped<ILifecycleNotificationSink, LifecycleNotificationSink>();

        if (!options.Enabled)
        {
            return services;
        }

        var factory = options.CreateConnectionFactory();
        services.AddSingleton(options);
        services.AddSingleton(factory);
        services.AddHostedService<RabbitNotificationConsumer>();
        return services;
    }
}
