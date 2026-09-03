using ApplicationService.Application.DTOs;

namespace ApplicationService.Application.Queries;

public sealed record GetEmployerApplicationsQuery(string JobId, int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<EmployerApplicationDto>>;
