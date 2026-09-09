namespace ApplicationService.Application.Recruitment;

public interface IRecruitmentClient
{
    Task ActivateCandidateProgressAsync(
        Guid candidateProfileId,
        string jobId,
        Guid applicationId,
        CancellationToken cancellationToken);
}
