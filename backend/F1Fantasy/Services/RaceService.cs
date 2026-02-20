using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class RaceService
{
    private readonly HttpClient _httpClient;
    private readonly RaceRepository _raceRepository;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public RaceService(HttpClient httpClient, RaceRepository raceRepository)
    {
        _httpClient = httpClient;
        _raceRepository = raceRepository;
    }

    public async Task<IEnumerable<Race>> GetRacesForSeasonAsync(string season)
    {
        var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{season}/races/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (apiResponse?.MRData?.RaceTable?.Races == null)
        {
            return Enumerable.Empty<Race>();
        }

        var races = apiResponse.MRData.RaceTable.Races;
        
        // Store in repository
        foreach (var race in races)
        {
            _raceRepository.AddOrUpdate(race);
        }

        return races;
    }

    public async Task<Race?> GetRaceByRoundAsync(string season, string round)
    {
        return await Task.FromResult(_raceRepository.GetByRound(season, round));
    }

    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        return await Task.FromResult(_raceRepository.GetAll());
    }
}