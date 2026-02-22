using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class ResultService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly ResultRepository _resultRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly ILogger<ResultService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public ResultService(
        HttpClient httpClient, 
        ResultRepository resultRepository,
        DataFetchMetadataRepository metadataRepository,
        RaceRepository raceRepository,
        ILogger<ResultService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _resultRepository = resultRepository;
        _metadataRepository = metadataRepository;
        _raceRepository = raceRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<RaceWithResults>> GetResultsBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching results for season {Season} from API", season);
        
        try
        {
            // Use limit=1000 to ensure we get all races in the season (typical F1 season has 20-24 races)
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/results.json?limit=1000");
            var apiResponse = JsonSerializer.Deserialize<ResultApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null)
            {
                _logger.LogWarning("API returned null response for season {Season} results. Falling back to cached data.", season);
                return await BuildRaceWithResultsFromCache(season);
            }

            var races = apiResponse.MRData.RaceTable.Races;
            _logger.LogInformation("Retrieved results for {Count} races in season {Season} from API", races.Count, season);

            // Store results in repository
            foreach (var race in races)
            {
                if (race.Results != null)
                {
                    foreach (var result in race.Results)
                    {
                        // Populate IDs from nested objects
                        result.DriverId = result.Driver?.DriverId ?? result.DriverId;
                        result.ConstructorId = result.Constructor?.ConstructorId ?? result.ConstructorId;
                        await _resultRepository.AddOrUpdateAsync(result, race.Season, race.Round);
                    }
                }
            }

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season} results. Returning cached data.", season);
            return await BuildRaceWithResultsFromCache(season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching results for season {Season}", season);
            throw;
        }
    }

    public async Task<RaceWithResults?> GetResultsByRaceAsync(string season, string round)
    {
        _logger.LogInformation("Fetching results for season {Season}, round {Round} from API", season, round);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/{round}/results/");
            var apiResponse = JsonSerializer.Deserialize<ResultApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null || !apiResponse.MRData.RaceTable.Races.Any())
            {
                _logger.LogWarning("API returned no results for season {Season}, round {Round}. Checking cache.", season, round);
                var cachedResults = await _resultRepository.GetByRaceAsync(season, round);
                if (cachedResults.Any())
                {
                    return new RaceWithResults
                    {
                        Season = season,
                        Round = round,
                        Results = cachedResults.ToList()
                    };
                }
                return null;
            }

            var race = apiResponse.MRData.RaceTable.Races.First();
            _logger.LogInformation("Retrieved {Count} results for season {Season}, round {Round} from API", 
                race.Results?.Count ?? 0, season, round);

            // Store results in repository
            if (race.Results != null)
            {
                foreach (var result in race.Results)
                {
                    // Populate IDs from nested objects
                    result.DriverId = result.Driver?.DriverId ?? result.DriverId;
                    result.ConstructorId = result.Constructor?.ConstructorId ?? result.ConstructorId;
                    await _resultRepository.AddOrUpdateAsync(result, race.Season, race.Round);
                }
            }

            return race;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}, round {Round} results. Returning cached data.", season, round);
            var cachedResults = await _resultRepository.GetByRaceAsync(season, round);
            if (cachedResults.Any())
            {
                return new RaceWithResults
                {
                    Season = season,
                    Round = round,
                    Results = cachedResults.ToList()
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching results for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    public async Task<Result?> GetResultByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogInformation("Fetching result for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        // Check cache first
        var cachedResult = await _resultRepository.GetByDriverAsync(season, round, driverId);
        if (cachedResult != null)
        {
            _logger.LogDebug("Result found in cache for driver {DriverId}", driverId);
            return cachedResult;
        }

        // If not in cache, fetch race results which will populate the cache
        _logger.LogDebug("Result not in cache, fetching race results to populate cache");
        var race = await GetResultsByRaceAsync(season, round);
        
        return await _resultRepository.GetByDriverAsync(season, round, driverId);
    }

    public async Task<IEnumerable<Result>> GetCachedResultsAsync()
    {
        _logger.LogDebug("Retrieving all cached results from repository");
        var results = await _resultRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached results", results.Count());
        return results;
    }

    public async Task<IEnumerable<RaceWithResults>> GetSprintResultsBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching sprint results for season {Season} from API", season);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/sprint/");
            var apiResponse = JsonSerializer.Deserialize<ResultApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null)
            {
                _logger.LogWarning("API returned null response for season {Season} sprint results. Falling back to cached data.", season);
                return await BuildSprintResultsFromCache(season);
            }

            var races = apiResponse.MRData.RaceTable.Races;
            _logger.LogInformation("Retrieved sprint results for {Count} races in season {Season} from API", races.Count, season);

            // Store sprint results in repository
            foreach (var race in races)
            {
                if (race.SprintResults != null)
                {
                    foreach (var result in race.SprintResults)
                    {
                        // Mark as sprint result
                        result.IsSprint = true;
                        // Populate IDs from nested objects
                        result.DriverId = result.Driver?.DriverId ?? result.DriverId;
                        result.ConstructorId = result.Constructor?.ConstructorId ?? result.ConstructorId;
                        await _resultRepository.AddOrUpdateAsync(result, race.Season, race.Round);
                    }
                }
            }

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season} sprint results. Returning cached data.", season);
            return await BuildSprintResultsFromCache(season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching sprint results for season {Season}", season);
            throw;
        }
    }

    public async Task<RaceWithResults?> GetSprintResultsByRaceAsync(string season, string round)
    {
        _logger.LogInformation("Fetching sprint results for season {Season}, round {Round} from API", season, round);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/{round}/sprint/");
            var apiResponse = JsonSerializer.Deserialize<ResultApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null || !apiResponse.MRData.RaceTable.Races.Any())
            {
                _logger.LogWarning("API returned no sprint results for season {Season}, round {Round}. Checking cache.", season, round);
                var cachedResults = await _resultRepository.GetSprintResultsByRaceAsync(season, round);
                if (cachedResults.Any())
                {
                    return new RaceWithResults
                    {
                        Season = season,
                        Round = round,
                        SprintResults = cachedResults.ToList()
                    };
                }
                return null;
            }

            var race = apiResponse.MRData.RaceTable.Races.First();
            _logger.LogInformation("Retrieved {Count} sprint results for season {Season}, round {Round} from API", 
                race.SprintResults?.Count ?? 0, season, round);

            // Store sprint results in repository
            if (race.SprintResults != null)
            {
                foreach (var result in race.SprintResults)
                {
                    // Mark as sprint result
                    result.IsSprint = true;
                    // Populate IDs from nested objects
                    result.DriverId = result.Driver?.DriverId ?? result.DriverId;
                    result.ConstructorId = result.Constructor?.ConstructorId ?? result.ConstructorId;
                    await _resultRepository.AddOrUpdateAsync(result, race.Season, race.Round);
                }
            }

            return race;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}, round {Round} sprint results. Returning cached data.", season, round);
            var cachedResults = await _resultRepository.GetSprintResultsByRaceAsync(season, round);
            if (cachedResults.Any())
            {
                return new RaceWithResults
                {
                    Season = season,
                    Round = round,
                    SprintResults = cachedResults.ToList()
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching sprint results for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    private async Task<IEnumerable<RaceWithResults>> BuildRaceWithResultsFromCache(string season)
    {
        var cachedResults = await _resultRepository.GetBySeasonAsync(season);
        
        // Populate Driver and Constructor navigation properties from IDs
        foreach (var r in cachedResults)
        {
            if (!string.IsNullOrEmpty(r.DriverId))
                r.Driver = new Driver { DriverId = r.DriverId };
            if (!string.IsNullOrEmpty(r.ConstructorId))
                r.Constructor = new Constructor { ConstructorId = r.ConstructorId };
        }
        
        var groupedByRace = cachedResults.GroupBy(r => new { r.Season, r.Round });

        return groupedByRace.Select(g => new RaceWithResults
        {
            Season = g.Key.Season,
            Round = g.Key.Round,
            Results = g.ToList()
        }).ToList();
    }

    private async Task<IEnumerable<RaceWithResults>> BuildSprintResultsFromCache(string season)
    {
        var cachedResults = await _resultRepository.GetSprintResultsBySeasonAsync(season);
        var groupedByRace = cachedResults.GroupBy(r => new { r.Season, r.Round });

        return groupedByRace.Select(g => new RaceWithResults
        {
            Season = g.Key.Season,
            Round = g.Key.Round,
            SprintResults = g.ToList()
        }).ToList();
    }

    public async Task<int?> GetLatestRoundWithResultsAsync(string season)
    {
        _logger.LogDebug("Getting latest round with results for season {Season}", season);
        
        // First check cache
        var cachedLatest = await _resultRepository.GetLatestRoundWithResultsAsync(season);
        
        // Check if we should fetch based on race schedule and last fetch time
        var shouldFetch = await ShouldFetchResultsAsync(season, cachedLatest);
        
        if (!shouldFetch && cachedLatest.HasValue)
        {
            _logger.LogInformation("Using cached latest round {Round} for season {Season}", cachedLatest, season);
            return cachedLatest;
        }
        
        // Only fetch from API if needed
        try
        {
            await GetResultsBySeasonAsync(season);
            var newLatest = await _resultRepository.GetLatestRoundWithResultsAsync(season);
            
            // Record fetch metadata
            await _metadataRepository.RecordFetchAsync(season, "Results", newLatest, true);
            
            return newLatest;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch latest results from API for season {Season}, using cached data", season);
            await _metadataRepository.RecordFetchAsync(season, "Results", cachedLatest, false, ex.Message);
            return cachedLatest;
        }
    }

    private async Task<bool> ShouldFetchResultsAsync(string season, int? cachedLatestRound)
    {
        // Check metadata for last fetch time
        var currentYear = DateTime.UtcNow.Year;
        var seasonYear = int.Parse(season);
        
        // For past seasons, results are final - less frequent fetching
        TimeSpan cacheExpiration = seasonYear < currentYear 
            ? TimeSpan.FromDays(7) 
            : TimeSpan.FromHours(1); // Current season - check more frequently
        
        var metadata = await _metadataRepository.GetMetadataAsync(season, "Results");
        
        if (metadata == null || !metadata.FetchSuccessful)
        {
            _logger.LogDebug("No valid metadata for Results/{Season}, should fetch", season);
            return true;
        }
        
        var age = DateTime.UtcNow - metadata.LastFetchedAt;
        if (age > cacheExpiration)
        {
            _logger.LogDebug("Results cache expired for season {Season} (age: {Age}), should fetch", season, age);
            return true;
        }
        
        // Check if there might be a new race since last fetch
        var races = await _raceRepository.GetBySeasonAsync(season);
        var racesSinceLastFetch = races
            .Where(r => DateTime.TryParse(r.Date, out var raceDate) && 
                       raceDate > metadata.LastFetchedAt &&
                       raceDate < DateTime.UtcNow.AddDays(1)) // Race is in the past (with 1 day buffer for results)
            .ToList();
        
        if (racesSinceLastFetch.Any())
        {
            _logger.LogInformation("Found {Count} race(s) since last fetch for season {Season}, should fetch results", 
                racesSinceLastFetch.Count, season);
            return true;
        }
        
        // Verify we have complete data for the cached latest round
        if (cachedLatestRound.HasValue)
        {
            var resultsForRound = await _resultRepository.GetByRaceAsync(season, cachedLatestRound.Value.ToString());
            if (!resultsForRound.Any())
            {
                _logger.LogWarning("Cached latest round {Round} has no results in DB, should fetch", cachedLatestRound);
                return true;
            }
        }
        
        _logger.LogDebug("Results cache valid for season {Season}, skip fetch", season);
        return false;
    }

    /// <summary>
    /// Get results from cache first, only fetch from API if not cached
    /// </summary>
    public async Task<IEnumerable<RaceWithResults>?> GetResultsBySeasonCachedAsync(string season)
    {
        _logger.LogDebug("Attempting to get results for season {Season} from cache first", season);
        
        // Check cache first
        var cached = await BuildRaceWithResultsFromCache(season);
        if (cached.Any())
        {
            _logger.LogInformation("Using {Count} cached race results for season {Season}", cached.Count(), season);
            return cached;
        }
        
        // Cache miss - fetch from API
        _logger.LogInformation("No cached results for season {Season}, fetching from API", season);
        return await GetResultsBySeasonAsync(season);
    }
}
