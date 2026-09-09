using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;

namespace ApplicationService.Infrastructure.Profiles;

public sealed class CompanyProfileClient(HttpClient httpClient, IHttpContextAccessor contextAccessor)
    : ICompanyProfileReader
{
    public async Task<CompanyProfileReference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/company-profiles/{Uri.EscapeDataString(userId)}");
        var authorization = contextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var bearer)
            && bearer.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = bearer;
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var profile = await response.Content.ReadFromJsonAsync<CompanyProfileReference>(cancellationToken);
            if (profile is null || profile.Id == Guid.Empty
                || !string.Equals(profile.UserId, userId, StringComparison.Ordinal))
            {
                throw new DependencyUnavailableException("Profile");
            }

            return profile;
        }
        catch (HttpRequestException exception)
        {
            throw new DependencyUnavailableException("Profile", exception);
        }
        catch (JsonException exception)
        {
            throw new DependencyUnavailableException("Profile", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependencyUnavailableException("Profile", exception);
        }
    }
}
