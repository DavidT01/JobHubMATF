using ApplicationService.Application;
using ApplicationService.Application.Commands;
using ApplicationService.Application.DTOs;
using ApplicationService.Application.Recruitment;
using ApplicationService.Infrastructure;
using ApplicationService.Infrastructure.Recruitment;
using ApplicationService.Persistence;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void ProductionRegistrations_ResolveApplicationHandlersAndRecruitmentClient()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
            ["JwtSettings:Secret"] = "test-only-signing-key-at-least-32-bytes",
            ["Services:ProfileBaseUrl"] = "http://localhost:5213",
            ["Services:ProfilePublicBaseUrl"] = "http://localhost:5213",
            ["Services:CatalogBaseUrl"] = "http://localhost:5246",
            ["GrpcServices:RecruitmentApi"] = "http://localhost:5116",
            ["ConnectionStrings:ApplicationDatabase"] = "Host=localhost;Database=test;Username=test;Password=test"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRouting();
        services.AddLogging();
        services.AddApplicationLayer(configuration);
        services.AddApplicationInfrastructure(configuration);
        services.AddApplicationPersistence(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;
        var recruitment = scoped.GetRequiredService<IRecruitmentClient>();
        Assert.IsType<RecruitmentClient>(recruitment);
        Assert.Same(recruitment, scoped.GetRequiredService<IRecruitmentClient>());
        Assert.NotNull(scoped.GetRequiredService<IRequestHandler<SubmitApplicationCommand, ApplicationListItemDto>>());
        Assert.NotNull(scoped.GetRequiredService<IRequestHandler<ChangeApplicationStatusCommand, Unit>>());
    }
}
