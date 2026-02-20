using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class RaceService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly RaceRepository _raceRepository;
    private readonly ILogger<RaceService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public RaceService(HttpClient httpClient, RaceRepository raceRepository, ILogger<RaceService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _raceRepository = raceRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Race>> GetRacesForSeasonAsync(string season)
    {
        _logger.LogInformation("Fetching races for season {Season} from API", season);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/races/");
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null)
            {
                _logger.LogWarning("API returned null response for season {Season}. Falling back to cached data.", season);
                // Fall back to cached data if API returns unexpected response
                return await _raceRepository.GetBySeasonAsync(season);
            }

            var races = apiResponse.MRData.RaceTable.Races;
            _logger.LogInformation("Retrieved {Count} races for season {Season} from API", races.Count, season);
            
            // Store in repository
            foreach (var race in races)
            {
                await _raceRepository.AddOrUpdateAsync(race);
            }

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}. Returning cached data.", season);
            // If API fails completely (even after retries), fall back to cached data
            return await _raceRepository.GetBySeasonAsync(season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching races for season {Season}", season);
            throw;
        }
    }

    public async Task<Race?> GetRaceByRoundAsync(string season, string round)
    {
        _logger.LogDebug("Fetching race for season {Season}, round {Round}", season, round);
        var race = await _raceRepository.GetByRoundAsync(season, round);
        
        if (race == null)
        {
            _logger.LogDebug("Race not found in cache for season {Season}, round {Round}", season, round);
        }
        
        return race;
    }

    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        _logger.LogDebug("Fetching all races from repository");
        var races = await _raceRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} total races", races.Count());
        return races;
    }
}