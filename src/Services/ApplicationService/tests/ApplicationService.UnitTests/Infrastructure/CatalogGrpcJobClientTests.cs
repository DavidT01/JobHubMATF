using ApplicationService.Application.Exceptions;
using ApplicationService.Infrastructure.Catalog;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using JobHub.Grpc.Contracts.Catalog;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class CatalogGrpcJobClientTests
{
    private const string JobId = "507f1f77bcf86cd799439011";
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Maps_job_and_nullable_expiry_and_sets_deadline(bool active)
    {
        var clock = new FixedClock();
        using var cancellation = new CancellationTokenSource();
        var invoker = new LookupInvoker((request, options) =>
        {
            Assert.Equal(JobId, request.JobId);
            Assert.Equal(clock.GetUtcNow().AddSeconds(10).UtcDateTime, options.Deadline);
            Assert.Equal(cancellation.Token, options.CancellationToken);
            return Task.FromResult(new ApplicationJobResponse
            {
                JobId = JobId.ToUpperInvariant(), CompanyId = CompanyId.ToString(), IsActive = active
            });
        });
        var result = await Reader(invoker, clock).GetByIdAsync(JobId, cancellation.Token);
        Assert.NotNull(result);
        Assert.Equal(CompanyId, result.CompanyId);
        Assert.Equal(active, result.IsActive);
        Assert.Null(result.ExpirationDate);
        Assert.True(invoker.Disposed);
    }

    [Fact]
    public async Task Preserves_expired_timestamp_for_business_validation()
    {
        var expiration = DateTimeOffset.UnixEpoch;
        var invoker = new LookupInvoker((_, _) => Task.FromResult(new ApplicationJobResponse
        {
            JobId = JobId, CompanyId = CompanyId.ToString(), ExpirationDate = Timestamp.FromDateTimeOffset(expiration)
        }));
        var result = await Reader(invoker).GetByIdAsync(JobId, TestContext.Current.CancellationToken);
        Assert.Equal(expiration, result!.ExpirationDate);
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Cancelled)]
    [InlineData(StatusCode.PermissionDenied)]
    [InlineData(StatusCode.Unauthenticated)]
    [InlineData(StatusCode.InvalidArgument)]
    [InlineData(StatusCode.Internal)]
    public async Task Rpc_errors_are_not_mistaken_for_missing_jobs(StatusCode status)
    {
        var invoker = ErrorInvoker(status);
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(invoker).GetByIdAsync(JobId, TestContext.Current.CancellationToken));
        Assert.True(invoker.Disposed);
    }

    [Fact]
    public async Task Only_not_found_returns_null()
    {
        Assert.Null(await Reader(ErrorInvoker(StatusCode.NotFound)).GetByIdAsync(JobId, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("wrong", "b6cd3fa8-25bf-42f4-9567-69362a4b5f7f")]
    [InlineData(JobId, "identity-id")]
    [InlineData(JobId, "00000000-0000-0000-0000-000000000000")]
    public async Task Inconsistent_references_fail_closed(string id, string company)
    {
        var invoker = new LookupInvoker((_, _) => Task.FromResult(new ApplicationJobResponse { JobId = id, CompanyId = company }));
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(invoker).GetByIdAsync(JobId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Malformed_timestamp_is_dependency_error()
    {
        var invoker = new LookupInvoker((_, _) => Task.FromResult(new ApplicationJobResponse
        {
            JobId = JobId, CompanyId = CompanyId.ToString(), ExpirationDate = new Timestamp { Nanos = -1 }
        }));
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(invoker).GetByIdAsync(JobId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Caller_cancellation_is_not_dependency_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var invoker = new LookupInvoker((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromException<ApplicationJobResponse>(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));
        });
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => Reader(invoker).GetByIdAsync(JobId, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    [Fact]
    public async Task Already_cancelled_request_does_not_call_catalog()
    {
        var invoker = new LookupInvoker((_, _) => throw new InvalidOperationException("Must not be called"));
        await Assert.ThrowsAsync<OperationCanceledException>(() => Reader(invoker).GetByIdAsync(JobId, new CancellationToken(true)));
    }

    private static CatalogGrpcJobClient Reader(LookupInvoker invoker, TimeProvider? clock = null) =>
        new(new CatalogJobGrpcService.CatalogJobGrpcServiceClient(invoker), clock ?? TimeProvider.System);

    private static LookupInvoker ErrorInvoker(StatusCode status) => new((_, _) =>
        Task.FromException<ApplicationJobResponse>(new RpcException(new Status(status, "failure"))));

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 11, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class LookupInvoker(Func<GetJobRequest, CallOptions, Task<ApplicationJobResponse>> lookup) : CallInvoker
    {
        public bool Disposed { get; private set; }
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
            string? host, CallOptions options, TRequest request)
        {
            Assert.Equal("/jobhub.catalog.v1.CatalogJobGrpcService/GetJob", method.FullName);
            var response = lookup((GetJobRequest)(object)request, options);
            return new AsyncUnaryCall<TResponse>((Task<TResponse>)(object)response, Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => Disposed = true);
        }
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
    }
}
