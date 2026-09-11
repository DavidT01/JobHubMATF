using Catalog.Entities;
using Catalog.Repositories;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using JobHub.Grpc.Contracts.Catalog;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Catalog.Services.Grpc;

public sealed class CatalogJobGrpcService(IJobRepository repository, ILogger<CatalogJobGrpcService> logger)
    : JobHub.Grpc.Contracts.Catalog.CatalogJobGrpcService.CatalogJobGrpcServiceBase
{
    public override async Task<ApplicationJobResponse> GetJob(GetJobRequest request, ServerCallContext context)
    {
        if (request.JobId.Length != 24 || !ObjectId.TryParse(request.JobId, out var objectId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "job_id must be a 24-character ObjectId."));

        Job? job;
        try
        {
            job = await repository.GetByIdAsync(objectId.ToString(), context.CancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Job lookup was cancelled."));
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException)
        {
            logger.LogWarning(exception, "Catalog storage is unavailable during job lookup.");
            throw new RpcException(new Status(StatusCode.Unavailable, "Catalog storage is unavailable."));
        }

        if (job is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Job was not found."));
        if (!string.Equals(job.Id, request.JobId, StringComparison.OrdinalIgnoreCase)
            || !Guid.TryParse(job.CompanyId, out var companyId) || companyId == Guid.Empty)
            throw new RpcException(new Status(StatusCode.Internal, "Job reference is inconsistent."));

        var response = new ApplicationJobResponse
        {
            JobId = objectId.ToString(), CompanyId = companyId.ToString(), IsActive = job.IsActive
        };
        if (job.ExpirationDate is { } expiration)
        {
            // Mongo dates are UTC. Unspecified values are interpreted as UTC,
            // never in the local time zone of the service container.
            var utc = expiration.Kind == DateTimeKind.Local
                ? expiration.ToUniversalTime()
                : DateTime.SpecifyKind(expiration, DateTimeKind.Utc);
            response.ExpirationDate = Timestamp.FromDateTime(utc);
        }
        return response;
    }
}
