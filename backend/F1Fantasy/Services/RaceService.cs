using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class RaceService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly RaceRepository _raceRepository;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public RaceService(HttpClient httpClient, RaceRepository raceRepository)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _raceRepository = raceRepository;
    }

    public async Task<IEnumerable<Race>> GetRacesForSeasonAsync(string season)
    {
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/races/");
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null)
            {
                // Fall back to cached data if API returns unexpected response
                return _raceRepository.GetBySeason(season);
            }

            var races = apiResponse.MRData.RaceTable.Races;
            
            // Store in repository
            foreach (var race in races)
            {
                _raceRepository.AddOrUpdate(race);
            }

            return races;
        }
        catch (HttpRequestException)
        {
            // If API fails completely (even after retries), fall back to cached data
            Console.WriteLine($"API call failed for season {season}. Returning cached data.");
            return _raceRepository.GetBySeason(season);
        }
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