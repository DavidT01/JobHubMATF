using System.Net;
using System.Text.Json;
using ApplicationService.Application.Catalog;
using ApplicationService.Application.Exceptions;

namespace ApplicationService.Infrastructure.Catalog;

public sealed class CatalogJobClient(HttpClient httpClient) : IJobReader
{
    public async Task<CatalogJob?> GetByIdAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"api/v1/Catalog/{Uri.EscapeDataString(jobId)}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var job = await response.Content.ReadFromJsonAsync<JobResponse>(cancellationToken);
            if (job is null || !string.Equals(job.Id, jobId, StringComparison.OrdinalIgnoreCase)
                || !Guid.TryParse(job.CompanyId, out var companyId) || companyId == Guid.Empty
                || job.IsActive is null)
            {
                throw new DependencyUnavailableException("Catalog");
            }

            return new CatalogJob(companyId, job.IsActive.Value, job.ExpirationDate);
        }
        catch (HttpRequestException exception)
        {
            throw new DependencyUnavailableException("Catalog", exception);
        }
        catch (JsonException exception)
        {
            throw new DependencyUnavailableException("Catalog", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DependencyUnavailableException("Catalog", exception);
        }
    }

    private sealed record JobResponse(string? Id, string? CompanyId, bool? IsActive, DateTimeOffset? ExpirationDate);
}
