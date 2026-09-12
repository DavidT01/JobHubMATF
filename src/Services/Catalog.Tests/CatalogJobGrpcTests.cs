using Catalog.Entities;
using Catalog.Repositories;
using Grpc.Core;
using Grpc.Net.Client;
using JobHub.Grpc.Contracts.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Moq;
using Xunit;
using Service = Catalog.Services.Grpc.CatalogJobGrpcService;

namespace Catalog.Tests;

public sealed class CatalogJobGrpcTests : IDisposable
{
    private const string JobId = "507f1f77bcf86cd799439011";
    private readonly Mock<IJobRepository> repository = new(MockBehavior.Strict);
    private readonly IHost host;
    private readonly GrpcChannel channel;
    private readonly CatalogJobGrpcService.CatalogJobGrpcServiceClient client;

    public CatalogJobGrpcTests()
    {
        host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddGrpc();
            services.AddSingleton(repository.Object);
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<Service>());
        })).Build();
        host.Start();
        channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = host.GetTestClient() });
        client = new CatalogJobGrpcService.CatalogJobGrpcServiceClient(channel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Lookup_returns_company_profile_and_active_flag_without_expiry(bool active)
    {
        var job = new Job { Id = JobId, CompanyId = Guid.NewGuid().ToString(), IsActive = active };
        SetupJob(job);
        var result = await client.GetJobAsync(new() { JobId = JobId.ToUpperInvariant() });
        Assert.Equal(JobId, result.JobId);
        Assert.Equal(job.CompanyId, result.CompanyId);
        Assert.Equal(active, result.IsActive);
        Assert.Null(result.ExpirationDate);
        repository.Verify(x => x.GetByIdAsync(JobId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task Expired_job_is_returned_with_correct_utc_timestamp(DateTimeKind kind)
    {
        var expiration = new DateTime(2020, 1, 1, 12, 0, 0, kind);
        SetupJob(new Job { Id = JobId, CompanyId = Guid.NewGuid().ToString(), ExpirationDate = expiration });
        var result = await client.GetJobAsync(new() { JobId = JobId });
        var expected = kind == DateTimeKind.Local ? expiration.ToUniversalTime() : DateTime.SpecifyKind(expiration, DateTimeKind.Utc);
        Assert.Equal(expected, result.ExpirationDate.ToDateTime());
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("507f1f77bcf86cd799439011 ")]
    public async Task Invalid_identifier_does_not_query_storage(string id)
    {
        await ExpectStatus(id, StatusCode.InvalidArgument);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Missing_job_is_not_found()
    {
        SetupJob(null);
        await ExpectStatus(JobId, StatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("identity-user-123")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Invalid_company_reference_is_not_returned(string companyId)
    {
        SetupJob(new Job { Id = JobId, CompanyId = companyId });
        await ExpectStatus(JobId, StatusCode.Internal);
    }

    [Fact]
    public async Task Wrong_job_reference_is_not_returned()
    {
        SetupJob(new Job { Id = "507f1f77bcf86cd799439012", CompanyId = Guid.NewGuid().ToString() });
        await ExpectStatus(JobId, StatusCode.Internal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Storage_failure_is_unavailable_not_missing(bool timeout)
    {
        repository.Setup(x => x.GetByIdAsync(JobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeout ? new TimeoutException("private storage detail") : new MongoException("private storage detail"));
        var error = await ExpectStatus(JobId, StatusCode.Unavailable);
        Assert.DoesNotContain("private storage detail", error.Status.Detail);
    }

    [Fact]
    public async Task Cancellation_reaches_repository()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Setup(x => x.GetByIdAsync(JobId, It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken token) =>
            {
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
                return (Job?)null;
            });
        using var cancellation = new CancellationTokenSource();
        using var call = client.GetJobAsync(new() { JobId = JobId }, cancellationToken: cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var error = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
        Assert.Equal(StatusCode.Cancelled, error.StatusCode);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private void SetupJob(Job? job) => repository.Setup(x => x.GetByIdAsync(JobId, It.IsAny<CancellationToken>())).ReturnsAsync(job);

    private async Task<RpcException> ExpectStatus(string id, StatusCode expected)
    {
        var error = await Assert.ThrowsAsync<RpcException>(async () => await client.GetJobAsync(new() { JobId = id }));
        Assert.Equal(expected, error.StatusCode);
        return error;
    }

    public void Dispose()
    {
        channel.Dispose();
        host.Dispose();
    }
}
