using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class LapTimingService
{
    private readonly HttpClient _httpClient;
    private readonly LapTimingRepository _repository;
    private readonly ILogger<LapTimingService> _logger;

    public LapTimingService(
        HttpClient httpClient,
        LapTimingRepository repository,
        ILogger<LapTimingService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.jolpi.ca/ergast/f1/");
        _repository = repository;
        _logger = logger;
    }

    public async Task<RaceWithLaps?> GetLapsByRaceAsync(string season, string round)
    {
        _logger.LogInformation("Fetching lap timings for season {Season}, round {Round}", season, round);
        
        try
        {
            var url = $"{season}/{round}/laps.json";
            _logger.LogDebug("Calling Ergast API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status {StatusCode} for lap timings: {Season}/{Round}", 
                    response.StatusCode, season, round);
                
                // Fallback to cached data
                return await BuildLapsFromCache(season, round);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var apiResponse = JsonSerializer.Deserialize<LapTimingApiResponse>(content, options);

            if (apiResponse?.MRData?.RaceTable?.Races == null || !apiResponse.MRData.RaceTable.Races.Any())
            {
                _logger.LogWarning("No lap timing data found for season {Season}, round {Round}", season, round);
                return await BuildLapsFromCache(season, round);
            }

            var race = apiResponse.MRData.RaceTable.Races.First();
            if (race.Laps == null || !race.Laps.Any())
            {
                _logger.LogWarning("No laps found in race data for season {Season}, round {Round}", season, round);
                return await BuildLapsFromCache(season, round);
            }

            _logger.LogDebug("Processing {LapCount} laps for season {Season}, round {Round}", 
                race.Laps.Count, season, round);

            // Flatten the nested structure: each lap has multiple timings (one per driver)
            foreach (var lap in race.Laps)
            {
                if (lap.Timings == null || !lap.Timings.Any())
                {
                    _logger.LogDebug("No timings found for lap {LapNumber}", lap.Number);
                    continue;
                }

                foreach (var timing in lap.Timings)
                {
                    var lapTiming = new LapTiming
                    {
                        Season = season,
                        Round = round,
                        LapNumber = lap.Number,
                        DriverId = timing.DriverId,
                        Position = timing.Position,
                        Time = timing.Time
                    };

                    await _repository.AddOrUpdateAsync(lapTiming, season, round);
                }
            }

            _logger.LogInformation("Successfully stored lap timings for {LapCount} laps, season {Season}, round {Round}", 
                race.Laps.Count, season, round);

            return await BuildLapsFromCache(season, round);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching lap timings for season {Season}, round {Round}", 
                season, round);
            return await BuildLapsFromCache(season, round);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse lap timing API response for season {Season}, round {Round}", 
                season, round);
            return await BuildLapsFromCache(season, round);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching lap timings for season {Season}, round {Round}", 
                season, round);
            throw;
        }
    }

    public async Task<Lap?> GetLapByNumberAsync(string season, string round, string lapNumber)
    {
        _logger.LogInformation("Fetching lap {LapNumber} for season {Season}, round {Round}", 
            lapNumber, season, round);
        
        try
        {
            // First try to get from cache
            var cachedTimings = await _repository.GetByLapAsync(season, round, lapNumber);
            if (cachedTimings.Any())
            {
                _logger.LogDebug("Found {Count} timings in cache for lap {LapNumber}", 
                    cachedTimings.Count(), lapNumber);
                return new Lap
                {
                    Number = lapNumber,
                    Timings = cachedTimings.ToList()
                };
            }

            // If not in cache, fetch all laps for the race
            _logger.LogDebug("Lap {LapNumber} not in cache, fetching all laps from API", lapNumber);
            await GetLapsByRaceAsync(season, round);

            // Try cache again
            cachedTimings = await _repository.GetByLapAsync(season, round, lapNumber);
            if (!cachedTimings.Any())
            {
                _logger.LogWarning("No timings found for lap {LapNumber} after API fetch", lapNumber);
                return null;
            }

            return new Lap
            {
                Number = lapNumber,
                Timings = cachedTimings.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lap {LapNumber} for season {Season}, round {Round}", 
                lapNumber, season, round);
            throw;
        }
    }

    public async Task<List<LapTiming>?> GetLapsByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogInformation("Fetching laps for driver {DriverId}, season {Season}, round {Round}", 
            driverId, season, round);
        
        try
        {
            // First try to get from cache
            var cachedTimings = await _repository.GetByDriverAsync(season, round, driverId);
            if (cachedTimings.Any())
            {
                _logger.LogDebug("Found {Count} laps in cache for driver {DriverId}", 
                    cachedTimings.Count(), driverId);
                return cachedTimings.ToList();
            }

            // If not in cache, fetch all laps for the race
            _logger.LogDebug("Driver {DriverId} laps not in cache, fetching from API", driverId);
            await GetLapsByRaceAsync(season, round);

            // Try cache again
            cachedTimings = await _repository.GetByDriverAsync(season, round, driverId);
            if (!cachedTimings.Any())
            {
                _logger.LogWarning("No laps found for driver {DriverId} after API fetch", driverId);
                return null;
            }

            return cachedTimings.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching laps for driver {DriverId}, season {Season}, round {Round}", 
                driverId, season, round);
            throw;
        }
    }

    public async Task<IEnumerable<LapTiming>> GetCachedLapsAsync()
    {
        _logger.LogInformation("Fetching all cached lap timings");
        
        try
        {
            var cachedLaps = await _repository.GetAllAsync();
            _logger.LogDebug("Found {Count} lap timings in cache", cachedLaps.Count());
            return cachedLaps;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached lap timings");
            throw;
        }
    }

    private async Task<RaceWithLaps?> BuildLapsFromCache(string season, string round)
    {
        _logger.LogDebug("Building lap data from cache for season {Season}, round {Round}", season, round);
        
        try
        {
            var cachedTimings = await _repository.GetByRaceAsync(season, round);
            if (!cachedTimings.Any())
            {
                _logger.LogWarning("No cached lap timings found for season {Season}, round {Round}", season, round);
                return null;
            }

            // Group by lap number
            var laps = cachedTimings
                .GroupBy(t => t.LapNumber)
                .Select(g => new Lap
                {
                    Number = g.Key,
                    Timings = g.OrderBy(t => int.Parse(t.Position)).ToList()
                })
                .OrderBy(l => int.Parse(l.Number))
                .ToList();

            _logger.LogDebug("Built {LapCount} laps from cache", laps.Count);

            return new RaceWithLaps
            {
                Season = season,
                Round = round,
                Laps = laps
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building laps from cache for season {Season}, round {Round}", 
                season, round);
            return null;
        }
    }
}
