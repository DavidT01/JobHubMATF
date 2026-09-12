using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Identity.API.Tests;

public class SecurityAdminTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SecurityAdminTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_FailsFiveTimes_ThenLocksAccount()
    {
        using var client = _factory.CreateClient();
        var email = $"lockout-{Guid.NewGuid():N}@example.com";
        await RegisterConfirmAsync(client, email);

        for (var i = 0; i < 5; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        var body = await locked.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_SearchUsers_FiltersByEmail()
    {
        using var client = _factory.CreateClient();
        var email = $"search-{Guid.NewGuid():N}@example.com";
        await RegisterConfirmAsync(client, email);
        await AuthorizeAsAdminAsync(client);

        var response = await client.GetAsync($"/api/admin/users?search={Uri.EscapeDataString(email[..12])}");
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == email);
    }

    [Fact]
    public async Task Admin_ConfirmEmail_AllowsLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"admin-confirm-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "No",
            lastName = "Confirm",
            email,
            password = "Pass123!",
            role = "Candidate"
        });
        var body = await register.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);

        var before = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);

        await AuthorizeAsAdminAsync(client);
        var confirm = await client.PostAsync($"/api/admin/users/{body!.UserId}/confirm-email", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var after = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task Admin_Stats_ReturnsTotals()
    {
        using var client = _factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var response = await client.GetAsync("/api/admin/stats");
        var stats = await response.Content.ReadFromJsonAsync<StatsDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(stats);
        Assert.True(stats.TotalUsers >= 1);
    }

    private static async Task RegisterConfirmAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "Pass123!",
            role = "Candidate"
        });
        var body = await register.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);
        await client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            userId = body!.UserId,
            token = body.EmailToken
        });
    }

    private static async Task AuthorizeAsAdminAsync(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@jobhub.local",
            password = "Admin123!"
        });
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens!.Token);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class RegisterResponse
    {
        public string? UserId { get; set; }
        public string? EmailToken { get; set; }
    }

    private sealed class AdminUserDto
    {
        public string Email { get; set; } = string.Empty;
    }

    private sealed class StatsDto
    {
        public int TotalUsers { get; set; }
    }
}
