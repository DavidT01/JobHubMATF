using System.Security.Claims;
using Grpc.Core;
using JobHub.Grpc.Contracts.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Profile.API.Features.CandidateProfiles.Queries.GetCandidateProfile;
using Profile.API.Features.CompanyProfiles.Queries.GetCompanyProfile;

namespace Profile.API.Services.GrpcServices;

[Authorize]
public sealed class ApplicationProfileGrpcService(ISender sender)
    : JobHub.Grpc.Contracts.Profile.ApplicationProfileGrpcService.ApplicationProfileGrpcServiceBase
{
    public override async Task<ApplicationCandidateProfileResponse> GetCandidateByUserId(
        GetApplicationProfileRequest request, ServerCallContext context)
    {
        RequireOwner(request.UserId, "Candidate", context);
        var profile = await QueryAsync(new GetCandidateProfileQuery(request.UserId), context.CancellationToken);
        if (profile is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Candidate profile was not found."));
        ValidateReference(profile.Id, profile.UserId, request.UserId);
        return new ApplicationCandidateProfileResponse
        {
            ProfileId = profile.Id.ToString(), UserId = profile.UserId, CvUrl = profile.CvUrl ?? ""
        };
    }

    public override async Task<ApplicationCompanyProfileResponse> GetCompanyByUserId(
        GetApplicationProfileRequest request, ServerCallContext context)
    {
        RequireOwner(request.UserId, "Employer", context);
        var profile = await QueryAsync(new GetCompanyProfileQuery(request.UserId), context.CancellationToken);
        if (profile is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Company profile was not found."));
        ValidateReference(profile.Id, profile.UserId, request.UserId);
        return new ApplicationCompanyProfileResponse { ProfileId = profile.Id.ToString(), UserId = profile.UserId };
    }

    private async Task<T> QueryAsync<T>(IRequest<T> query, CancellationToken cancellationToken)
    {
        try { return await sender.Send(query, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Profile lookup was cancelled."));
        }
    }

    private static void RequireOwner(string userId, string role, ServerCallContext context)
    {
        var user = context.GetHttpContext().User;
        if (user.Identity?.IsAuthenticated != true)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Authentication is required."));
        if (string.IsNullOrWhiteSpace(userId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id is required."));
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        var hasRole = user.IsInRole(role) || user.HasClaim(ClaimTypes.Role, role);
        if (!hasRole || !string.Equals(subject, userId, StringComparison.Ordinal))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only the profile owner can access this lookup."));
    }

    private static void ValidateReference(Guid profileId, string actualUserId, string requestedUserId)
    {
        if (profileId == Guid.Empty || !string.Equals(actualUserId, requestedUserId, StringComparison.Ordinal))
            throw new RpcException(new Status(StatusCode.Internal, "Profile reference is inconsistent."));
    }
}
