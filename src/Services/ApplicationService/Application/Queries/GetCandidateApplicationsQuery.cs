using ApplicationService.Application.DTOs;

namespace ApplicationService.Application.Queries;

public sealed record GetCandidateApplicationsQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedResult<ApplicationListItemDto>>;
