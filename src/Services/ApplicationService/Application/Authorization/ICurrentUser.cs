namespace ApplicationService.Application.Authorization;

public interface ICurrentUser
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
