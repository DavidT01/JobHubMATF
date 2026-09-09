using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Recruitment;
using Grpc.Core;
using JobHub.Grpc.Contracts.Recruitment;

namespace ApplicationService.Infrastructure.Recruitment;

public sealed class RecruitmentClient(
    RecruitmentGrpcService.RecruitmentGrpcServiceClient client,
    IHttpContextAccessor contextAccessor)
    : IRecruitmentClient
{
    public async Task ActivateCandidateProgressAsync(
        Guid candidateProfileId,
        string jobId,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var request = new ActivateCandidateProgressRequest
        {
            CandidateProfileId = candidateProfileId.ToString("D"),
            JobId = jobId,
            ApplicationId = applicationId.ToString("D")
        };

        var metadata = new Metadata();
        var authorization = contextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
        {
            metadata.Add("authorization", authorization);
        }

        try
        {
            await client.ActivateCandidateProgressAsync(
                request,
                metadata,
                cancellationToken: cancellationToken);
        }
        catch (RpcException exception)
        {
            throw new DependencyUnavailableException("Recruitment", exception);
        }
    }
}
