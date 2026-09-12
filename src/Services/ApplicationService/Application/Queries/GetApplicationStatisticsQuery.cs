using ApplicationService.Application.DTOs;

namespace ApplicationService.Application.Queries;

public sealed record GetApplicationStatisticsQuery(DateOnly? From = null, DateOnly? To = null)
    : IQuery<ApplicationStatisticsDto>;
