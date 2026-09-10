namespace Recruitment.API.Infrastructure;

public interface IRecruitmentAuthorization
{
    Task EnsureCompanyAsync(Guid companyId, CancellationToken cancellationToken);

    Task EnsureProcessOwnerAsync(Guid processId, CancellationToken cancellationToken);

    Task EnsureProcessOwnerByJobIdAsync(string jobId, CancellationToken cancellationToken);

    Task EnsureRoundOwnerAsync(Guid selectionRoundId, CancellationToken cancellationToken);

    Task EnsureInterviewOwnerAsync(Guid interviewScheduleId, CancellationToken cancellationToken);

    Task EnsureEvaluationOwnerAsync(Guid candidateProfileId, CancellationToken cancellationToken);

    Task EnsureCandidateOwnsProfileAsync(Guid candidateProfileId, CancellationToken cancellationToken);
}
