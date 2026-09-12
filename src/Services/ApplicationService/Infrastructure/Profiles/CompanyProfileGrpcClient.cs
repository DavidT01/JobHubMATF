using System.Net.Http.Headers;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;
using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;

namespace ApplicationService.Infrastructure.Profiles;

public sealed class CompanyProfileGrpcClient(
    ApplicationProfileGrpcService.ApplicationProfileGrpcServiceClient client,
    IHttpContextAccessor contextAccessor, TimeProvider timeProvider) : ICompanyProfileReader
{
    public async Task<CompanyProfileReference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authorization = contextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out var bearer)
            || !bearer.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(bearer.Parameter)
            || bearer.Parameter.Any(c => c > 127 || char.IsWhiteSpace(c)))
            throw new DependencyUnavailableException("Profile");

        // Per-call metadata, never shared default headers between users.
        var headers = new Metadata { { "authorization", "Bearer " + bearer.Parameter } };
        try
        {
            using var call = client.GetCompanyByUserIdAsync(new GetApplicationProfileRequest { UserId = userId },
                headers, timeProvider.GetUtcNow().AddSeconds(10).UtcDateTime, cancellationToken);
            var profile = await call.ResponseAsync;
            if (!Guid.TryParse(profile.ProfileId, out var profileId) || profileId == Guid.Empty
                || !string.Equals(profile.UserId, userId, StringComparison.Ordinal))
                throw new DependencyUnavailableException("Profile");
            return new CompanyProfileReference(profileId, profile.UserId);
        }
        catch (RpcException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Company profile lookup was cancelled.", exception, cancellationToken);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException exception)
        {
            throw new DependencyUnavailableException("Profile", exception);
        }
    }
}
