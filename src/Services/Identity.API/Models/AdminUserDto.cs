namespace Identity.API.Models;

public class AdminUserDto
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required IList<string> Roles { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockedOut { get; set; }
}

public class SetUserRoleDto
{
    public required string Role { get; set; }
}
