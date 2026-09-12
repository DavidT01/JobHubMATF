using System.Security.Claims;
using ApplicationService.Application.Authorization;

namespace ApplicationService.Infrastructure.Authorization;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public string? UserId => IsAuthenticated
        ? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")
        : null;

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User.IsInRole(role);
}
