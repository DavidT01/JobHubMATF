namespace ApplicationService.Application.Catalog;

public interface IJobReader
{
    Task<CatalogJob?> GetByIdAsync(string jobId, CancellationToken cancellationToken);
}

public sealed record CatalogJob(Guid CompanyId, bool IsActive, DateTimeOffset? ExpirationDate);
