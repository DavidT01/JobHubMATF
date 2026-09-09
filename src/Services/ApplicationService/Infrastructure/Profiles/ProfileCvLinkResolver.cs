using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;

namespace ApplicationService.Infrastructure.Profiles;

public sealed class ProfileCvLinkResolver(IConfiguration configuration) : ICvLinkResolver
{
    public string Resolve(string cvPath)
    {
        const string prefix = "/uploads/cvs/";
        if (!cvPath.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new DependencyUnavailableException("Profile");
        }

        var fileName = cvPath[prefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".."
            || fileName.IndexOfAny(['/', '\\', '?', '#', '%']) >= 0)
        {
            throw new DependencyUnavailableException("Profile");
        }

        var baseUrl = configuration["Services:ProfilePublicBaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Services:ProfilePublicBaseUrl must be a public HTTP(S) base URL.");
        }

        // Only the configured Profile origin/path can be used, never a URL supplied by a candidate.
        var root = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
        return new Uri(root, $"uploads/cvs/{Uri.EscapeDataString(fileName)}").AbsoluteUri;
    }
}
