using Catalog.DTOs;

namespace Catalog.Clients;

public class ProfileApiClient : IProfileApiClient
{
    private readonly HttpClient _httpClient;
    public ProfileApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CandidateProfileDto?> GetCandidateByIdAsync(string candidateId)
    {
        var response = _httpClient.GetAsync($"/api/candidates/{candidateId}");
        if (!response.IsCompletedSuccessfully)
        {
            return null;
        }
        
        return await response.Result.Content.ReadFromJsonAsync<CandidateProfileDto>();
    }
}