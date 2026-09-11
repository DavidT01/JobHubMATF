using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using JobHub.Grpc.Contracts.Profile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Profile.API.DTOs;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile;
using Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile;
using Profile.API.Services.GrpcServices;
using Service = Profile.API.Services.GrpcServices.ApplicationProfileGrpcService;
using Contract = JobHub.Grpc.Contracts.Profile.ApplicationProfileGrpcService;

namespace Profile.UnitTests.Grpc;

public sealed class ApplicationProfileGrpcTests : IDisposable
{
    private const string Secret = "profile-grpc-tests-only-secret-at-least-32-bytes";
    private readonly Mock<ISender> sender = new(MockBehavior.Strict);
    private readonly TestServer server;
    private readonly IHost host;
    private readonly GrpcChannel channel;
    private readonly Contract.ApplicationProfileGrpcServiceClient client;

    public ApplicationProfileGrpcTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["JwtSettings:Secret"] = Secret }).Build();
        host = new HostBuilder().ConfigureWebHost(web => web.UseTestServer().ConfigureServices(services =>
        {
            services.AddGrpc();
            services.AddApplicationProfileAuthentication(configuration);
            services.AddSingleton(sender.Object);
        }).Configure(app =>
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapGrpcService<Service>());
        })).Build();
        host.Start();
        server = host.GetTestServer();
        channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpClient = server.CreateClient()
        });
        client = new Contract.ApplicationProfileGrpcServiceClient(channel);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/uploads/cvs/current.pdf")]
    public async Task Candidate_returns_current_cv_and_distinct_profile_id(string cvUrl)
    {
        var profile = new CandidateProfileDto { Id = Guid.NewGuid(), UserId = "candidate", CvUrl = cvUrl };
        sender.Setup(x => x.Send(It.Is<GetCandidateProfileQuery>(q => q.UserId == "candidate"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var result = await client.GetCandidateByUserIdAsync(new() { UserId = "candidate" }, Headers("candidate", "Candidate"));
        Assert.Equal(profile.Id.ToString(), result.ProfileId);
        Assert.Equal(profile.UserId, result.UserId);
        Assert.Equal(cvUrl, result.CvUrl);
    }

    [Fact]
    public async Task Company_returns_profile_id_not_identity_id()
    {
        var profile = new CompanyProfileDto { Id = Guid.NewGuid(), UserId = "company" };
        sender.Setup(x => x.Send(It.Is<GetCompanyProfileQuery>(q => q.UserId == "company"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var result = await client.GetCompanyByUserIdAsync(new() { UserId = "company" }, Headers("company", "Employer"));
        Assert.Equal(profile.Id.ToString(), result.ProfileId);
        Assert.Equal("company", result.UserId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_profile_returns_not_found(bool company)
    {
        if (company)
            sender.Setup(x => x.Send(It.IsAny<GetCompanyProfileQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((CompanyProfileDto?)null);
        else
            sender.Setup(x => x.Send(It.IsAny<GetCandidateProfileQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((CandidateProfileDto?)null);
        await ExpectStatus(company, "owner", Headers("owner", company ? "Employer" : "Candidate"), StatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Anonymous_cannot_read_profiles(bool company)
    {
        await ExpectStatus(company, "owner", new Metadata(), StatusCode.Unauthenticated);
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, "other", "Candidate")]
    [InlineData(false, "owner", "Employer")]
    [InlineData(true, "other", "Employer")]
    [InlineData(true, "owner", "Candidate")]
    [InlineData(false, "owner", "Admin")]
    public async Task Non_owner_or_wrong_role_is_denied_before_query(bool company, string subject, string role)
    {
        await ExpectStatus(company, "owner", Headers(subject, role), StatusCode.PermissionDenied);
        sender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    public async Task Invalid_token_is_rejected(string fault)
    {
        await ExpectStatus(false, "owner", Headers("owner", "Candidate", fault), StatusCode.Unauthenticated);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Empty_user_id_is_invalid()
    {
        await ExpectStatus(false, " ", Headers("owner", "Candidate"), StatusCode.InvalidArgument);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Inconsistent_profile_is_not_returned()
    {
        sender.Setup(x => x.Send(It.IsAny<GetCandidateProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CandidateProfileDto { Id = Guid.NewGuid(), UserId = "other" });
        await ExpectStatus(false, "owner", Headers("owner", "Candidate"), StatusCode.Internal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    public void Missing_or_weak_signing_secret_fails_configuration(string? secret)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["JwtSettings:Secret"] = secret }).Build();
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddApplicationProfileAuthentication(configuration));
    }

    [Fact]
    public async Task Caller_cancellation_reaches_profile_query()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sender.Setup(x => x.Send(It.IsAny<GetCandidateProfileQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetCandidateProfileQuery _, CancellationToken token) =>
            {
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (OperationCanceledException) { cancelled.TrySetResult(); throw; }
                return (CandidateProfileDto?)null;
            });
        using var cancellation = new CancellationTokenSource();
        using var call = client.GetCandidateByUserIdAsync(new() { UserId = "owner" },
            Headers("owner", "Candidate"), cancellationToken: cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var error = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
        Assert.Equal(StatusCode.Cancelled, error.StatusCode);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task ExpectStatus(bool company, string userId, Metadata headers, StatusCode expected)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            if (company) await client.GetCompanyByUserIdAsync(new() { UserId = userId }, headers);
            else await client.GetCandidateByUserIdAsync(new() { UserId = userId }, headers);
        });
        Assert.Equal(expected, exception.StatusCode);
    }

    private static Metadata Headers(string subject, string role, string? fault = null)
    {
        var token = new JwtSecurityToken(
            issuer: fault == "issuer" ? "wrong" : "JobHubIdentityAPI",
            audience: fault == "audience" ? "wrong" : "JobHubClients",
            claims: [new Claim("sub", subject), new Claim("role", role)],
            notBefore: DateTime.UtcNow.AddHours(-1),
            expires: fault == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                fault == "signature" ? Secret + "wrong" : Secret)), SecurityAlgorithms.HmacSha256));
        return new Metadata { { "authorization", "Bearer " + new JwtSecurityTokenHandler().WriteToken(token) } };
    }

    public void Dispose()
    {
        channel.Dispose();
        host.Dispose();
    }
}
