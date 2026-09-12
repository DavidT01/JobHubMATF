using ApplicationService.Application.Catalog;
using ApplicationService.Application.Exceptions;
using Grpc.Core;
using JobHub.Grpc.Contracts.Catalog;

namespace ApplicationService.Infrastructure.Catalog;

public sealed class CatalogGrpcJobClient(CatalogJobGrpcService.CatalogJobGrpcServiceClient client,
    TimeProvider timeProvider) : IJobReader
{
    public async Task<CatalogJob?> GetByIdAsync(string jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var call = client.GetJobAsync(new GetJobRequest { JobId = jobId },
                deadline: timeProvider.GetUtcNow().AddSeconds(10).UtcDateTime,
                cancellationToken: cancellationToken);
            var job = await call.ResponseAsync;
            if (!string.Equals(job.JobId, jobId, StringComparison.OrdinalIgnoreCase)
                || !Guid.TryParse(job.CompanyId, out var companyId) || companyId == Guid.Empty)
                throw new DependencyUnavailableException("Catalog");

            return new CatalogJob(companyId, job.IsActive, job.ExpirationDate?.ToDateTimeOffset());
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Catalog lookup was cancelled.", exception, cancellationToken);
        }
        catch (RpcException exception)
        {
            throw new DependencyUnavailableException("Catalog", exception);
        }
        catch (InvalidOperationException exception)
        {
            // A malformed protobuf timestamp must not masquerade as a valid job.
            throw new DependencyUnavailableException("Catalog", exception);
        }
    }
}
