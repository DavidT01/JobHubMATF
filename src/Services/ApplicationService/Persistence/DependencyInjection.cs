using ApplicationService.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApplicationDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'ApplicationDatabase' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("application-database");

        return services;
    }
}
