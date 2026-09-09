namespace Identity.API.Models;

public class MeResponse
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required IList<string> Roles { get; set; }
}
