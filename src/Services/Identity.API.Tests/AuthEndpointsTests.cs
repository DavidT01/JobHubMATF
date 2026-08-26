using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Identity.API.Tests;

public class AuthEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Candidate_ReturnsSuccess()
    {
        using var client = _factory.CreateClient();
        var response = await RegisterAsync(client, $"cand-{Guid.NewGuid():N}@example.com", "Candidate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Admin_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var payload = new
        {
            firstName = "Admin",
            lastName = "User",
            email = $"admin-{Guid.NewGuid():N}@example.com",
            password = "Pass123!",
            role = "Admin"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "Candidate");

        var second = await RegisterAsync(client, email, "Employer");

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwt()
    {
        using var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "Employer");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body?.Token));
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var email = $"bad-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "Candidate");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUser()
    {
        using var client = _factory.CreateClient();
        var email = $"me-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "Candidate");

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass123!" });
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens!.Token);

        var response = await client.GetAsync("/api/auth/me");
        var me = await response.Content.ReadFromJsonAsync<MeDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, me?.Email);
        Assert.Contains("Candidate", me?.Roles ?? []);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string role)
    {
        var payload = new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "Pass123!",
            role
        };

        return await client.PostAsJsonAsync("/api/auth/register", payload);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class MeDto
    {
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = [];
    }
}
