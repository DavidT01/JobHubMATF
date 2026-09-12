using ApplicationService.Application.DTOs;
using ApplicationService.Application.Exceptions;
using ApplicationService.Application.Queries;
using ApplicationService.Domain.Enums;
using ApplicationService.Persistence.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Application.Handlers;

public sealed class GetApplicationStatisticsHandler(ApplicationDbContext dbContext)
    : IRequestHandler<GetApplicationStatisticsQuery, ApplicationStatisticsDto>
{
    public async Task<ApplicationStatisticsDto> Handle(
        GetApplicationStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["from"] = ["From date must be earlier than or equal to the to date."]
            });
        }

        var query = dbContext.JobApplications.AsNoTracking();
        if (request.From.HasValue)
        {
            var fromUtc = new DateTimeOffset(
                request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            query = query.Where(application => application.SubmittedAtUtc >= fromUtc);
        }

        if (request.To.HasValue)
        {
            var toUtc = new DateTimeOffset(
                request.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            query = query.Where(application => application.SubmittedAtUtc <= toUtc);
        }

        var counts = await query
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var totalCount = counts.Values.Sum();

        var byStatus = Enum.GetValues<ApplicationStatus>()
            .Select(status =>
            {
                var count = counts.GetValueOrDefault(status);
                var rate = totalCount == 0
                    ? 0m
                    : decimal.Round(count * 100m / totalCount, 2, MidpointRounding.AwayFromZero);
                return new ApplicationStatusStatisticsDto(status, count, rate);
            })
            .ToArray();

        var dailyRows = await query
            .GroupBy(application => application.SubmittedAtUtc.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);
        var dailyTrend = dailyRows
            .Select(item => new DailyApplicationCountDto(DateOnly.FromDateTime(item.Date), item.Count))
            .ToArray();

        return new ApplicationStatisticsDto(
            totalCount, request.From, request.To, byStatus, dailyTrend);
    }
}
