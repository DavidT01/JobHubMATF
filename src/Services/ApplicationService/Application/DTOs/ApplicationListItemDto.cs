using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.DTOs;

public sealed record ApplicationListItemDto(
    Guid Id,
    string JobId,
    ApplicationStatus Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset UpdatedAtUtc);
