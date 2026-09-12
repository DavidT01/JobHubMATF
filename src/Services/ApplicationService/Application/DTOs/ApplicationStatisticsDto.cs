using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.DTOs;

public sealed record ApplicationStatusStatisticsDto(
    ApplicationStatus Status,
    int Count,
    decimal RatePercent);

public sealed record DailyApplicationCountDto(DateOnly Date, int Count);

public sealed record ApplicationStatisticsDto(
    int TotalCount,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<ApplicationStatusStatisticsDto> ByStatus,
    IReadOnlyList<DailyApplicationCountDto> DailyTrend);
