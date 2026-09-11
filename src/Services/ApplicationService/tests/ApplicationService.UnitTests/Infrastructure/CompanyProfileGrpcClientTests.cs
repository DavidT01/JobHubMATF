using ApplicationService.Application.Exceptions;
using ApplicationService.Infrastructure.Profiles;
using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ApplicationService.UnitTests.Infrastructure;

public sealed class CompanyProfileGrpcClientTests
{
    [Fact]
    public async Task Maps_profile_and_forwards_only_current_bearer_with_deadline()
    {
        var id = Guid.NewGuid();
        var accessor = Context("bEaReR first-token");
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var invoker = new LookupInvoker((request, options) =>
        {
            Assert.Equal("owner", request.UserId);
            Assert.Equal(cancellation.Token, options.CancellationToken);
            Assert.InRange(options.Deadline!.Value, DateTime.UtcNow.AddSeconds(8), DateTime.UtcNow.AddSeconds(11));
            Assert.Single(options.Headers!);
            Assert.Equal(calls++ == 0 ? "Bearer first-token" : "Bearer second-token", options.Headers!.GetValue("authorization"));
            return Task.FromResult(new ApplicationCompanyProfileResponse { ProfileId = id.ToString(), UserId = "owner" });
        });
        var reader = Reader(invoker, accessor);
        var profile = await reader.GetByUserIdAsync("owner", cancellation.Token);
        Assert.Equal(id, profile!.Id);
        Assert.Equal("owner", profile.UserId);
        accessor.HttpContext!.Request.Headers.Authorization = "Bearer second-token";
        await reader.GetByUserIdAsync("owner", cancellation.Token);
        Assert.Equal(2, calls);
        Assert.Equal(2, invoker.Disposals);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic password")]
    [InlineData("Bearer")]
    [InlineData("Bearer a b")]
    public async Task Missing_or_invalid_bearer_does_not_call_profile(string? header)
    {
        var invoker = new LookupInvoker((_, _) => throw new Exception("Must not call"));
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(invoker, Context(header))
            .GetByUserIdAsync("owner", TestContext.Current.CancellationToken));
        Assert.Equal(0, invoker.Disposals);
    }

    [Theory]
    [InlineData(StatusCode.Unauthenticated)]
    [InlineData(StatusCode.PermissionDenied)]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Cancelled)]
    public async Task Rpc_failures_are_dependency_errors(StatusCode code)
    {
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(Error(code))
            .GetByUserIdAsync("owner", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Not_found_is_null() => Assert.Null(await Reader(Error(StatusCode.NotFound))
        .GetByUserIdAsync("owner", TestContext.Current.CancellationToken));

    [Theory]
    [InlineData("bad-id", "owner")]
    [InlineData("00000000-0000-0000-0000-000000000000", "owner")]
    [InlineData("b6cd3fa8-25bf-42f4-9567-69362a4b5f7f", "other")]
    public async Task Inconsistent_profile_reference_is_rejected(string id, string userId)
    {
        var invoker = new LookupInvoker((_, _) => Task.FromResult(new ApplicationCompanyProfileResponse { ProfileId = id, UserId = userId }));
        await Assert.ThrowsAsync<DependencyUnavailableException>(() => Reader(invoker)
            .GetByUserIdAsync("owner", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Caller_cancellation_remains_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var invoker = new LookupInvoker((_, _) =>
        {
            cancellation.Cancel();
            return Task.FromException<ApplicationCompanyProfileResponse>(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));
        });
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => Reader(invoker).GetByUserIdAsync("owner", cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    private static HttpContextAccessor Context(string? token)
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        if (token is not null) accessor.HttpContext.Request.Headers.Authorization = token;
        return accessor;
    }
    private static CompanyProfileGrpcClient Reader(LookupInvoker invoker, HttpContextAccessor? accessor = null) =>
        new(new ApplicationProfileGrpcService.ApplicationProfileGrpcServiceClient(invoker), accessor ?? Context("Bearer test-token"), TimeProvider.System);
    private static LookupInvoker Error(StatusCode code) => new((_, _) =>
        Task.FromException<ApplicationCompanyProfileResponse>(new RpcException(new Status(code, "failure"))));

    private sealed class LookupInvoker(Func<GetApplicationProfileRequest, CallOptions, Task<ApplicationCompanyProfileResponse>> lookup) : CallInvoker
    {
        public int Disposals { get; private set; }
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method,
            string? host, CallOptions options, TRequest request)
        {
            Assert.Equal("/jobhub.profile.v1.ApplicationProfileGrpcService/GetCompanyByUserId", method.FullName);
            var response = lookup((GetApplicationProfileRequest)(object)request, options);
            return new AsyncUnaryCall<TResponse>((Task<TResponse>)(object)response, Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => Disposals++);
        }
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) => throw new NotSupportedException();
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) => throw new NotSupportedException();
    }
}
