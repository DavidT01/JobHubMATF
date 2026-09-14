using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.DTOs;

public sealed record CandidateApplicationDetailsDto(
    Guid Id,
    Guid CandidateProfileId,
    string JobId,
    ApplicationStatus Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset UpdatedAtUtc);