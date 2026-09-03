using ApplicationService.Application.Authorization;
using ApplicationService.Application.DTOs;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Profiles;
using ApplicationService.Application.Queries;
using ApplicationService.Domain.Entities;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Application.Handlers;

public sealed class GetEmployerApplicationsHandler(
    ApplicationDbContext dbContext, JobOwnershipGuard ownership,
    ICandidateProfileReader profileReader, ICvLinkResolver cvLinks)
    : IRequestHandler<GetEmployerApplicationsQuery, PagedResult<EmployerApplicationDto>>
{
    public async Task<PagedResult<EmployerApplicationDto>> Handle(
        GetEmployerApplicationsQuery request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.JobId) || request.JobId.Length != JobApplication.CatalogJobIdLength
            || !request.JobId.All(Uri.IsHexDigit))
        {
            errors["jobId"] = ["Job identifier must be a 24-character hexadecimal Catalog identifier."];
        }
        if (request.PageNumber < 1) errors["pageNumber"] = ["Page number must be at least 1."];
        if (request.PageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
        var offset = ((long)request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue) errors["pageNumber"] = ["Requested page is too large."];
        if (errors.Count > 0) throw new RequestValidationException(errors);

        var jobId = request.JobId.ToLowerInvariant();
        // No applications or candidate profiles are read until company ownership is established.
        await ownership.EnsureOwnedAsync(jobId, cancellationToken);
        var query = dbContext.JobApplications.AsNoTracking().Where(application => application.JobId == jobId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(application => application.SubmittedAtUtc)
            .ThenBy(application => application.Id).Skip((int)offset).Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = new EmployerApplicationDto[rows.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, rows.Count),
            new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                var row = rows[index];
                string? name = null;
                string? cv = null;
                var cvStatus = CurrentCvStatus.ProfileReferenceMissing;
                if (!string.IsNullOrWhiteSpace(row.CandidateUserId))
                {
                    var profile = await profileReader.GetByUserIdAsync(row.CandidateUserId, token);
                    cvStatus = CurrentCvStatus.ProfileMissing;
                    if (profile is not null)
                    {
                        if (profile.Id != row.CandidateId) throw new DependencyUnavailableException("Profile");
                        name = string.Join(" ", new[] { profile.FirstName, profile.LastName }
                            .Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
                        if (name.Length == 0) name = null;
                        cvStatus = CurrentCvStatus.Missing;
                        if (!string.IsNullOrWhiteSpace(profile.CvUrl))
                        {
                            cv = cvLinks.Resolve(profile.CvUrl);
                            cvStatus = CurrentCvStatus.Available;
                        }
                    }
                }

                items[index] = new EmployerApplicationDto(row.Id, row.JobId, row.CandidateId, name,
                    row.CoverLetter, row.Status, row.SubmittedAtUtc, row.UpdatedAtUtc, cv, cvStatus);
            });
        return new PagedResult<EmployerApplicationDto>(items, total, request.PageNumber, request.PageSize);
    }
}
