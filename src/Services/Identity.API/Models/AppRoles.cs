namespace Identity.API.Models;

public static class AppRoles
{
    public const string Candidate = "Candidate";
    public const string Employer = "Employer";
    public const string Admin = "Admin";

    public static readonly HashSet<string> Registrable = new(StringComparer.OrdinalIgnoreCase)
    {
        Candidate,
        Employer
    };
}
