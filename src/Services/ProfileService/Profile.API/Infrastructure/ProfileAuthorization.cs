using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Profile.API.Data;
using Profile.API.Exceptions;

namespace Profile.API.Infrastructure;

public sealed class ProfileAuthorization(
    IHttpContextAccessor httpContextAccessor,
    IProfileContext context) : IProfileAuthorization
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public string GetCurrentUserId()
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ProfileForbiddenException();
        }

        return userId;
    }

    public async Task EnsureCanReadCandidateProfileAsync(string userId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Employer"))
        {
            return;
        }

        if (!string.Equals(GetCurrentUserId(), userId, StringComparison.Ordinal))
        {
            throw new ProfileForbiddenException();
        }

        await Task.CompletedTask;
    }

    public async Task EnsureCandidateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return;
        }

        var userId = GetCurrentUserId();
        var ownsProfile = await context.CandidateProfiles
            .AnyAsync(profile => profile.Id == profileId && profile.UserId == userId, cancellationToken);
        if (!ownsProfile)
        {
            throw new ProfileForbiddenException();
        }
    }

    public async Task EnsureCompanyProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return;
        }

        var userId = GetCurrentUserId();
        var ownsProfile = await context.CompanyProfiles
            .AnyAsync(profile => profile.Id == profileId && profile.UserId == userId, cancellationToken);
        if (!ownsProfile)
        {
            throw new ProfileForbiddenException();
        }
    }
}
