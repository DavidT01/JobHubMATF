using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Identity.API.Tests;

public class AdminNotificationsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AdminNotificationsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminLogin_ThenListUsers_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        await AuthorizeAsAdminAsync(client);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserDto>>(JsonOptions);
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Email == "admin@jobhub.local");
    }

    [Fact]
    public async Task Candidate_CannotAccessAdminUsers()
    {
        using var client = _factory.CreateClient();
        var email = $"cand-admin-{Guid.NewGuid():N}@example.com";
        await RegisterConfirmLoginAsync(client, email, "Candidate");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesWelcomeNotification_AfterLogin()
    {
        using var client = _factory.CreateClient();
        var email = $"notif-{Guid.NewGuid():N}@example.com";
        await RegisterConfirmLoginAsync(client, email, "Candidate");

        var response = await client.GetAsync("/api/notifications");
        var items = await response.Content.ReadFromJsonAsync<List<NotificationDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Contains(items, n => n.Title.Contains("Welcome", StringComparison.OrdinalIgnoreCase)
            || n.Title.Contains("Email confirmed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MarkAllRead_ClearsUnreadCount()
    {
        using var client = _factory.CreateClient();
        var email = $"read-{Guid.NewGuid():N}@example.com";
        await RegisterConfirmLoginAsync(client, email, "Employer");

        var before = await client.GetFromJsonAsync<UnreadDto>("/api/notifications/unread-count", JsonOptions);
        Assert.True(before!.Count > 0);

        var mark = await client.PostAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, mark.StatusCode);

        var after = await client.GetFromJsonAsync<UnreadDto>("/api/notifications/unread-count", JsonOptions);
        Assert.Equal(0, after!.Count);
    }

    [Fact]
    public async Task Admin_CanLockAndUnlockUser()
    {
        using var client = _factory.CreateClient();
        var email = $"lock-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Lock",
            lastName = "Me",
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

        await AuthorizeAsAdminAsync(client);

        var lockResponse = await client.PostAsync($"/api/admin/users/{body.UserId}/lock", null);
        Assert.Equal(HttpStatusCode.OK, lockResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var lockedLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedLogin.StatusCode);

        await AuthorizeAsAdminAsync(client);
        var unlockResponse = await client.PostAsync($"/api/admin/users/{body.UserId}/unlock", null);
        Assert.Equal(HttpStatusCode.OK, unlockResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var unlockedLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        Assert.Equal(HttpStatusCode.OK, unlockedLogin.StatusCode);
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

    private static async Task RegisterConfirmLoginAsync(HttpClient client, string email, string role)
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "Pass123!",
            role
        });
        var body = await register.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);

        await client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            userId = body!.UserId,
            token = body.EmailToken
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
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

    private sealed class NotificationDto
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class UnreadDto
    {
        public int Count { get; set; }
    }
}
