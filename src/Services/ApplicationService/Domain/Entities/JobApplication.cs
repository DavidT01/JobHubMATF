using ApplicationService.Domain.Enums;
using ApplicationService.Domain.Exceptions;

namespace ApplicationService.Domain.Entities;

public sealed class JobApplication
{
    public const int MaximumCoverLetterLength = 5_000;
    public const int CatalogJobIdLength = 24;

    private JobApplication()
    {
    }

    private JobApplication(
        Guid id,
        Guid candidateId,
        string jobId,
        string? coverLetter,
        DateTimeOffset submittedAtUtc)
    {
        Id = id;
        CandidateId = candidateId;
        JobId = jobId;
        CoverLetter = NormalizeCoverLetter(coverLetter);
        Status = ApplicationStatus.Submitted;
        SubmittedAtUtc = submittedAtUtc.ToUniversalTime();
        UpdatedAtUtc = SubmittedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CandidateId { get; private set; }

    public string JobId { get; private set; } = null!;

    public string? CoverLetter { get; private set; }

    public ApplicationStatus Status { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static JobApplication Create(
        Guid candidateId,
        string jobId,
        string? coverLetter,
        DateTimeOffset submittedAtUtc)
    {
        if (candidateId == Guid.Empty)
        {
            throw new ApplicationDomainException("Candidate identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(jobId)
            || jobId.Length != CatalogJobIdLength
            || !jobId.All(Uri.IsHexDigit))
        {
            throw new ApplicationDomainException("Job identifier must be a 24-character hexadecimal Catalog identifier.");
        }

        return new JobApplication(
            Guid.NewGuid(),
            candidateId,
            jobId.ToLowerInvariant(),
            coverLetter,
            submittedAtUtc);
    }

    public void ChangeStatus(ApplicationStatus newStatus, DateTimeOffset changedAtUtc)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new ApplicationDomainException(
                $"Application status cannot change from {Status} to {newStatus}.");
        }

        var normalizedChangedAt = changedAtUtc.ToUniversalTime();
        if (normalizedChangedAt < UpdatedAtUtc)
        {
            throw new ApplicationDomainException(
                "Status change time cannot be earlier than the previous update time.");
        }

        Status = newStatus;
        UpdatedAtUtc = normalizedChangedAt;
    }

    public bool CanTransitionTo(ApplicationStatus newStatus)
    {
        return Status switch
        {
            ApplicationStatus.Submitted => newStatus is
                ApplicationStatus.InReview or ApplicationStatus.Rejected,
            ApplicationStatus.InReview => newStatus is
                ApplicationStatus.Interview or ApplicationStatus.Accepted or ApplicationStatus.Rejected,
            ApplicationStatus.Interview => newStatus is
                ApplicationStatus.Accepted or ApplicationStatus.Rejected,
            ApplicationStatus.Rejected or ApplicationStatus.Accepted => false,
            _ => false
        };
    }

    private static string? NormalizeCoverLetter(string? coverLetter)
    {
        if (string.IsNullOrWhiteSpace(coverLetter))
        {
            return null;
        }

        var normalizedCoverLetter = coverLetter.Trim();
        if (normalizedCoverLetter.Length > MaximumCoverLetterLength)
        {
            throw new ApplicationDomainException(
                $"Cover letter cannot exceed {MaximumCoverLetterLength} characters.");
        }

        return normalizedCoverLetter;
    }
}
