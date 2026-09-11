using ApplicationService.Application.Catalog;
using ApplicationService.Application.Profiles;
using ApplicationService.Infrastructure;
using ApplicationService.Infrastructure.Catalog;
using ApplicationService.Infrastructure.Profiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class GrpcReaderRegistrationTests
{
    [Fact]
    public void Production_registration_resolves_scoped_grpc_readers_without_network_calls()
    {
        using var provider = Provider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var company = first.ServiceProvider.GetRequiredService<ICompanyProfileReader>();
        var job = first.ServiceProvider.GetRequiredService<IJobReader>();
        Assert.IsType<CompanyProfileGrpcClient>(company);
        Assert.IsType<CatalogGrpcJobClient>(job);
        Assert.Same(company, first.ServiceProvider.GetRequiredService<ICompanyProfileReader>());
        Assert.Same(job, first.ServiceProvider.GetRequiredService<IJobReader>());
        Assert.NotSame(company, second.ServiceProvider.GetRequiredService<ICompanyProfileReader>());
        Assert.NotSame(job, second.ServiceProvider.GetRequiredService<IJobReader>());
        // The applicant-access decision is still pending; preserve the HTTP reader.
        Assert.IsType<CandidateProfileClient>(first.ServiceProvider.GetRequiredService<ICandidateProfileReader>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData("http://profile:8080")]
    [InlineData("https://user:password@profile")]
    [InlineData("https://profile/api")]
    [InlineData("https://profile/?token=value")]
    [InlineData("https://profile/#fragment")]
    public void Profile_rejects_unsafe_or_ambiguous_origins(string? address)
    {
        using var provider = Provider("GrpcServices:ProfileApi", address);
        using var scope = provider.CreateScope();
        var error = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ICompanyProfileReader>());
        Assert.Contains("GrpcServices:ProfileApi", error.Message);
        Assert.DoesNotContain("password", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData("ftp://catalog")]
    [InlineData("https://user:password@catalog")]
    [InlineData("https://catalog/api")]
    [InlineData("https://catalog/?q=value")]
    [InlineData("https://catalog/#fragment")]
    public void Catalog_rejects_invalid_origins(string? address)
    {
        using var provider = Provider("GrpcServices:CatalogApi", address);
        using var scope = provider.CreateScope();
        var error = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IJobReader>());
        Assert.Contains("GrpcServices:CatalogApi", error.Message);
    }

    [Theory]
    [InlineData("http://catalog:8081")]
    [InlineData("https://catalog:8443/")]
    public void Catalog_accepts_explicit_internal_http_or_https_origin(string address)
    {
        using var provider = Provider("GrpcServices:CatalogApi", address);
        using var scope = provider.CreateScope();
        Assert.IsType<CatalogGrpcJobClient>(scope.ServiceProvider.GetRequiredService<IJobReader>());
    }

    private static ServiceProvider Provider(string? key = null, string? value = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "JobHubIdentityAPI",
            ["JwtSettings:Audience"] = "JobHubClients",
            ["JwtSettings:Secret"] = "grpc-di-tests-only-secret-at-least-32-bytes",
            ["Services:ProfileBaseUrl"] = "http://localhost:5213",
            ["Services:ProfilePublicBaseUrl"] = "http://localhost:5213",
            ["GrpcServices:ProfileApi"] = "https://localhost:7043",
            ["GrpcServices:CatalogApi"] = "https://localhost:7018",
            ["GrpcServices:RecruitmentApi"] = "http://localhost:5116"
        };
        if (key is not null) settings[key] = value;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddRouting();
        services.AddApplicationInfrastructure(configuration);
        // This test resolves only the reader graph, not unrelated command handlers.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
