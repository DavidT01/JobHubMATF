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
        var response = await _httpClient.GetAsync($"/api/candidate-profiles/{candidateId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        
        return await response.Content.ReadFromJsonAsync<CandidateProfileDto>();
    }
}