namespace ApplicationService.Application.Profiles;

public interface ICandidateProfileReader
{
    Task<CandidateProfileReference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken);
}

public sealed record CandidateProfileReference(
    Guid Id, string UserId, string? CvUrl, string? FirstName, string? LastName);
