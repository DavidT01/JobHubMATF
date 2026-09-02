using System.Text;
using System.Security.Claims;
using ApplicationService.Application.Authorization;
using ApplicationService.Infrastructure.Authorization;
using ApplicationService.Application.Profiles;
using ApplicationService.Infrastructure.Profiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ApplicationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetRequiredSection("JwtSettings");
        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException("JwtSettings:Issuer is not configured.");
        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException("JwtSettings:Audience is not configured.");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. Provide it through user secrets or an environment variable.");

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JwtSettings:Issuer and JwtSettings:Audience must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("JwtSettings:Secret must contain at least 32 UTF-8 bytes.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddHttpClient<ICandidateProfileReader, CandidateProfileClient>(client =>
        {
            var baseUrl = configuration["Services:ProfileBaseUrl"];
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("Services:ProfileBaseUrl must be an absolute HTTP(S) URL.");
            }

            client.BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = false;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)))
                        {
                            context.Fail("The token does not identify a user.");
                        }

                        return Task.CompletedTask;
                    }
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Candidate,
                policy => policy.RequireAuthenticatedUser().RequireRole(JobHubRoles.Candidate))
            .AddPolicy(AuthorizationPolicies.Employer,
                policy => policy.RequireAuthenticatedUser().RequireRole(JobHubRoles.Employer));

        return services;
    }
}
