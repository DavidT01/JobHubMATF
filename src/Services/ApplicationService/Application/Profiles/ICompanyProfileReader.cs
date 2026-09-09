namespace ApplicationService.Application.Profiles;

public interface ICompanyProfileReader
{
    Task<CompanyProfileReference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
}

public sealed record CompanyProfileReference(Guid Id, string UserId);
