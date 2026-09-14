using System.Net.Http.Headers;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;
using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;

namespace ApplicationService.Infrastructure.Profiles;

public sealed class CandidateProfileClient(
    ApplicationProfileGrpcService.ApplicationProfileGrpcServiceClient client,
    IHttpContextAccessor contextAccessor,
    TimeProvider timeProvider)
    : ICandidateProfileReader
{
    public async Task<CandidateProfileReference?> GetByUserIdAsync(
        string userId, CancellationToken cancellationToken)
    {
        var authorization = contextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (AuthenticationHeaderValue.TryParse(authorization, out var bearer)
            && bearer.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(bearer.Parameter)
            && bearer.Parameter.All(c => c <= 127 && !char.IsWhiteSpace(c)))
        {
            var headers = new Metadata { { "authorization", "Bearer " + bearer.Parameter } };
            try
            {
                using var call = client.GetCandidateByUserIdAsync(
                    new GetApplicationProfileRequest { UserId = userId },
                    headers,
                    timeProvider.GetUtcNow().AddSeconds(10).UtcDateTime,
                    cancellationToken);
                var profile = await call.ResponseAsync;
                if (!Guid.TryParse(profile.ProfileId, out var profileId)
                    || profileId == Guid.Empty
                    || !string.Equals(profile.UserId, userId, StringComparison.Ordinal))
                {
                    throw new DependencyUnavailableException("Profile");
                }

                return new CandidateProfileReference(
                    profileId, profile.UserId, profile.CvUrl, profile.FirstName, profile.LastName);
            }
            catch (RpcException exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Candidate profile lookup was cancelled.", exception, cancellationToken);
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

        throw new DependencyUnavailableException("Profile");
    }
}
