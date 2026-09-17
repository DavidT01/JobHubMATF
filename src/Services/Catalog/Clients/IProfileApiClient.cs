using Catalog.DTOs;

namespace Catalog.Clients;

public interface IProfileApiClient
{
    Task<CandidateProfileDto?> GetCandidateByIdAsync(string candidateId);
    Task<List<CandidateProfileDto>> SearchCandidatesAsync(List<string> skills, string? location, int limit);
}