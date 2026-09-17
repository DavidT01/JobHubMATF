using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Notification.API.Data;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Notification.API.Tests;

public class NotificationEndpointsTests : IClassFixture<NotificationApiFactory>
{
    private const string ApiKey = "JobHubNotificationInternalKey-ChangeInProduction";
    private readonly NotificationApiFactory _factory;

    public NotificationEndpointsTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ThenList_ReturnsNotification_ForSameUser()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.NewGuid().ToString("N");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/notifications");
        createRequest.Headers.TryAddWithoutValidation("X-Api-Key", ApiKey);
        createRequest.Content = JsonContent.Create(new
        {
            userId,
            title = "Hello",
            message = "First notification"
        });
        var create = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Accepted, create.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(userId));
        var list = await client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications");
        Assert.NotNull(list);
        Assert.Contains(list, n => n.Title == "Hello" && n.Message == "First notification");
    }

    [Fact]
    public async Task Create_WithoutApiKey_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/notifications", new
        {
            userId = Guid.NewGuid().ToString("N"),
            title = "Nope",
            message = "No key"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateToken(string userId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("SuperSecretKeyForJobHubIdentityApiThatIsAtLeast32BytesLong!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "JobHubIdentityAPI",
            audience: "JobHubClients",
            claims: [new Claim(ClaimTypes.NameIdentifier, userId)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

public class NotificationApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotificationApi:ApiKey"] = "JobHubNotificationInternalKey-ChangeInProduction",
                ["JwtSettings:Secret"] = "SuperSecretKeyForJobHubIdentityApiThatIsAtLeast32BytesLong!",
                ["JwtSettings:Issuer"] = "JobHubIdentityAPI",
                ["JwtSettings:Audience"] = "JobHubClients",
                ["RabbitMq:Enabled"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<NotificationDbContext>)
                            || d.ServiceType == typeof(NotificationDbContext))
                .ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            var dbName = $"notif-tests-{Guid.NewGuid():N}.db";
            services.AddDbContext<NotificationDbContext>(options =>
                options.UseSqlite($"Data Source={dbName}"));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
