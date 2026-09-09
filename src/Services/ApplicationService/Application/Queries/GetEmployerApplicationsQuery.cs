using ApplicationService.Application.DTOs;
using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.Queries;

public sealed record GetEmployerApplicationsQuery(
    string JobId,
    int PageNumber = 1,
    int PageSize = 20,
    ApplicationStatus? Status = null,
    ApplicationSortBy SortBy = ApplicationSortBy.SubmittedAtUtc,
    SortDirection SortDirection = SortDirection.Desc)
    : IQuery<PagedResult<EmployerApplicationDto>>;
