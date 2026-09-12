using Catalog.DTOs;

namespace Catalog.Clients;

public interface IProfileApiClient
{
    Task<CandidateProfileDto?> GetCandidateByIdAsync(string candidateId);
}