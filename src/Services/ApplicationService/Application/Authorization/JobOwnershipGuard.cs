using ApplicationService.Application.Catalog;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;

namespace ApplicationService.Application.Authorization;

public sealed class JobOwnershipGuard(
    ICurrentUser currentUser, ICompanyProfileReader companyReader, IJobReader jobReader)
{
    public async Task EnsureOwnedAsync(string jobId, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(userId)
            || !currentUser.IsInRole("Employer"))
        {
            throw new ForbiddenException();
        }

        var company = await companyReader.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Company profile not found.");
        var job = await jobReader.GetByIdAsync(jobId, cancellationToken)
            ?? throw new ResourceNotFoundException("Job not found.");
        if (job.CompanyId != company.Id)
        {
            throw new ForbiddenException();
        }
    }
}
