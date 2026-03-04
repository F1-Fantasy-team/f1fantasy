using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class PitStopService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly PitStopRepository _pitStopRepository;
    private readonly ILogger<PitStopService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public PitStopService(
        HttpClient httpClient, 
        PitStopRepository pitStopRepository, 
        ILogger<PitStopService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _pitStopRepository = pitStopRepository;
        _logger = logger;
    }

    public async Task<RaceWithPitStops?> GetPitStopsByRaceAsync(string season, string round)
    {
        _logger.LogInformation("Fetching pit stops for season {Season}, round {Round} from API", season, round);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/{round}/pitstops/");
            var apiResponse = JsonSerializer.Deserialize<PitStopApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null || !apiResponse.MRData.RaceTable.Races.Any())
            {
                _logger.LogWarning("API returned no pit stops for season {Season}, round {Round}. Checking cache.", season, round);
                var cachedPitStops = await _pitStopRepository.GetByRaceAsync(season, round);
                if (cachedPitStops.Any())
                {
                    return new RaceWithPitStops
                    {
                        Season = season,
                        Round = round,
                        PitStops = cachedPitStops.ToList()
                    };
                }
                return null;
            }

            var race = apiResponse.MRData.RaceTable.Races.First();
            _logger.LogInformation("Retrieved {Count} pit stops for season {Season}, round {Round} from API", 
                race.PitStops?.Count ?? 0, season, round);

            // Store pit stops in repository using batch operation
            if (race.PitStops != null && race.PitStops.Any())
            {
                await _pitStopRepository.AddOrUpdateBatchAsync(race.PitStops, race.Season, race.Round);
            }

            return race;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}, round {Round} pit stops. Returning cached data.", season, round);
            var cachedPitStops = await _pitStopRepository.GetByRaceAsync(season, round);
            if (cachedPitStops.Any())
            {
                return new RaceWithPitStops
                {
                    Season = season,
                    Round = round,
                    PitStops = cachedPitStops.ToList()
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching pit stops for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    public async Task<IEnumerable<PitStop>> GetPitStopsByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogInformation("Fetching pit stops for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        // Check cache first
        var cachedPitStops = await _pitStopRepository.GetByDriverAsync(season, round, driverId);
        if (cachedPitStops.Any())
        {
            _logger.LogDebug("Pit stops found in cache for driver {DriverId}", driverId);
            return cachedPitStops;
        }

        // If not in cache, fetch race pit stops which will populate the cache
        _logger.LogDebug("Pit stops not in cache, fetching race pit stops to populate cache");
        var race = await GetPitStopsByRaceAsync(season, round);
        
        return await _pitStopRepository.GetByDriverAsync(season, round, driverId);
    }

    public async Task<IEnumerable<PitStop>> GetCachedPitStopsAsync()
    {
        _logger.LogDebug("Retrieving all cached pit stops from repository");
        var pitStops = await _pitStopRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached pit stops", pitStops.Count());
        return pitStops;
    }
}
