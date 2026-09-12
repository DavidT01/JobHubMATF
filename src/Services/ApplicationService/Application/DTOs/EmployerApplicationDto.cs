using ApplicationService.Domain.Enums;

namespace ApplicationService.Application.DTOs;

public enum CurrentCvStatus { Available, Missing, ProfileMissing, ProfileReferenceMissing }

public sealed record EmployerApplicationDto(
    Guid Id, string JobId, Guid CandidateId, string? CandidateName, string? CoverLetter,
    ApplicationStatus Status, DateTimeOffset SubmittedAtUtc, DateTimeOffset UpdatedAtUtc,
    string? CurrentCvUrl, CurrentCvStatus CvStatus);
