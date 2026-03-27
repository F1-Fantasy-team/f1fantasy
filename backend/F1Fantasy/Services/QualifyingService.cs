using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class QualifyingService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly ILogger<QualifyingService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public QualifyingService(
        HttpClient httpClient, 
        QualifyingRepository qualifyingRepository,
        DataFetchMetadataRepository metadataRepository,
        RaceRepository raceRepository,
        ILogger<QualifyingService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _qualifyingRepository = qualifyingRepository;
        _metadataRepository = metadataRepository;
        _raceRepository = raceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Smart cache-first: Returns cached qualifying data if still valid, otherwise fetches from API
    /// </summary>
    public async Task<IEnumerable<RaceWithQualifying>> GetQualifyingBySeasonCachedAsync(string season)
    {
        _logger.LogInformation("Checking cache for qualifying results for season {Season}", season);
        
        // Check if we should fetch based on staleness
        var shouldFetch = await ShouldFetchQualifyingAsync(season);
        
        if (!shouldFetch)
        {
            var cachedData = await BuildQualifyingFromCache(season);
            if (cachedData.Any())
            {
                _logger.LogInformation("Returning cached qualifying data for season {Season} ({Count} races)", season, cachedData.Count());
                return cachedData;
            }
        }

        _logger.LogInformation("Cache stale or missing for season {Season}, fetching from API", season);
        return await GetQualifyingBySeasonAsync(season);
    }
    
    private async Task<bool> ShouldFetchQualifyingAsync(string season)
    {
        // Check metadata for last fetch time
        var currentYear = DateTime.UtcNow.Year;
        var seasonYear = int.Parse(season);
        
        // For past seasons, qualifying data is final - less frequent fetching
        TimeSpan cacheExpiration = seasonYear < currentYear 
            ? TimeSpan.FromDays(7) 
            : TimeSpan.FromHours(1); // Current season - check more frequently
        
        var metadata = await _metadataRepository.GetMetadataAsync(season, "Qualifying");
        
        if (metadata == null || !metadata.FetchSuccessful)
        {
            _logger.LogDebug("No valid metadata for Qualifying/{Season}, should fetch", season);
            return true;
        }
        
        var age = DateTime.UtcNow - metadata.LastFetchedAt;
        if (age > cacheExpiration)
        {
            _logger.LogDebug("Qualifying cache expired for season {Season} (age: {Age}), should fetch", season, age);
            return true;
        }
        
        // Check if there might be a new race since last fetch
        var races = await _raceRepository.GetBySeasonAsync(season);
        var racesSinceLastFetch = races
            .Where(r => DateTime.TryParse(r.Date, out var raceDate) && 
                       raceDate > metadata.LastFetchedAt &&
                       raceDate < DateTime.UtcNow) // Qualifying happens before race
            .ToList();
        
        if (racesSinceLastFetch.Any())
        {
            _logger.LogInformation("Found {Count} race(s) since last fetch for season {Season}, should fetch qualifying", 
                racesSinceLastFetch.Count, season);
            return true;
        }
        
        _logger.LogDebug("Qualifying cache valid for season {Season}, skip fetch", season);
        return false;
    }

    public async Task<IEnumerable<RaceWithQualifying>> GetQualifyingBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching qualifying results for season {Season} from API", season);
        
        try
        {
            // Use limit=1000 to ensure we get all races in the season (typical F1 season has 20-24 races)
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/qualifying.json?limit=1000");
            var apiResponse = JsonSerializer.Deserialize<QualifyingApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null)
            {
                _logger.LogWarning("API returned null response for season {Season} qualifying. Falling back to cached data.", season);
                return await BuildQualifyingFromCache(season);
            }

            var races = apiResponse.MRData.RaceTable.Races;
            _logger.LogInformation("Retrieved qualifying for {Count} races in season {Season} from API", races.Count, season);

            // Store qualifying results in repository using batch operations
            foreach (var race in races)
            {
                if (race.QualifyingResults != null && race.QualifyingResults.Any())
                {
                    // Populate IDs from nested objects
                    foreach (var qualifying in race.QualifyingResults)
                    {
                        qualifying.DriverId = qualifying.Driver?.DriverId ?? qualifying.DriverId;
                        qualifying.ConstructorId = qualifying.Constructor?.ConstructorId ?? qualifying.ConstructorId;
                    }
                    
                    // Batch save all qualifying results for this race
                    await _qualifyingRepository.AddOrUpdateBatchAsync(race.QualifyingResults, race.Season, race.Round);
                }
            }

            // Record fetch metadata
            var latestRound = races.Any() ? 
                (int.TryParse(races.Max(r => r.Round), out var maxRound) ? maxRound : (int?)null) : 
                null;
            await _metadataRepository.RecordFetchAsync(season, "Qualifying", latestRound, true);

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season} qualifying. Returning cached data.", season);
            await _metadataRepository.RecordFetchAsync(season, "Qualifying", null, false, ex.Message);
            return await BuildQualifyingFromCache(season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching qualifying for season {Season}", season);
            throw;
        }
    }

    public async Task<RaceWithQualifying?> GetQualifyingByRaceAsync(string season, string round)
    {
        _logger.LogInformation("Fetching qualifying for season {Season}, round {Round} from API", season, round);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/{round}/qualifying/");
            var apiResponse = JsonSerializer.Deserialize<QualifyingApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.RaceTable?.Races == null || !apiResponse.MRData.RaceTable.Races.Any())
            {
                _logger.LogWarning("API returned no qualifying for season {Season}, round {Round}. Checking cache.", season, round);
                var cachedQualifying = await _qualifyingRepository.GetByRaceAsync(season, round);
                if (cachedQualifying.Any())
                {
                    return new RaceWithQualifying
                    {
                        Season = season,
                        Round = round,
                        QualifyingResults = cachedQualifying.ToList()
                    };
                }
                return null;
            }

            var race = apiResponse.MRData.RaceTable.Races.First();
            _logger.LogInformation("Retrieved {Count} qualifying results for season {Season}, round {Round} from API", 
                race.QualifyingResults?.Count ?? 0, season, round);

            // Store qualifying results in repository
            if (race.QualifyingResults != null)
            {
                foreach (var qualifying in race.QualifyingResults)
                {
                    // Populate IDs from nested objects
                    qualifying.DriverId = qualifying.Driver?.DriverId ?? qualifying.DriverId;
                    qualifying.ConstructorId = qualifying.Constructor?.ConstructorId ?? qualifying.ConstructorId;
                    await _qualifyingRepository.AddOrUpdateAsync(qualifying, race.Season, race.Round);
                }
            }

            return race;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}, round {Round} qualifying. Returning cached data.", season, round);
            var cachedQualifying = await _qualifyingRepository.GetByRaceAsync(season, round);
            if (cachedQualifying.Any())
            {
                return new RaceWithQualifying
                {
                    Season = season,
                    Round = round,
                    QualifyingResults = cachedQualifying.ToList()
                };
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching qualifying for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    public async Task<Qualifying?> GetQualifyingByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogInformation("Fetching qualifying for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        // Check cache first
        var cachedQualifying = await _qualifyingRepository.GetByDriverAsync(season, round, driverId);
        if (cachedQualifying != null)
        {
            _logger.LogDebug("Qualifying found in cache for driver {DriverId}", driverId);
            return cachedQualifying;
        }

        // If not in cache, fetch race qualifying which will populate the cache
        _logger.LogDebug("Qualifying not in cache, fetching race qualifying to populate cache");
        var race = await GetQualifyingByRaceAsync(season, round);
        
        return await _qualifyingRepository.GetByDriverAsync(season, round, driverId);
    }

    public async Task<IEnumerable<Qualifying>> GetCachedQualifyingAsync()
    {
        _logger.LogDebug("Retrieving all cached qualifying from repository");
        var qualifyings = await _qualifyingRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached qualifying results", qualifyings.Count());
        return qualifyings;
    }

    private async Task<IEnumerable<RaceWithQualifying>> BuildQualifyingFromCache(string season)
    {
        var cachedQualifying = await _qualifyingRepository.GetBySeasonAsync(season);
        
        // Populate Driver and Constructor navigation properties from IDs
        foreach (var q in cachedQualifying)
        {
            if (!string.IsNullOrEmpty(q.DriverId))
                q.Driver = new Driver { DriverId = q.DriverId };
            if (!string.IsNullOrEmpty(q.ConstructorId))
                q.Constructor = new Constructor { ConstructorId = q.ConstructorId };
        }
        
        var groupedByRace = cachedQualifying.GroupBy(q => new { q.Season, q.Round });

        return groupedByRace.Select(g => new RaceWithQualifying
        {
            Season = g.Key.Season,
            Round = g.Key.Round,
            QualifyingResults = g.ToList()
        }).ToList();
    }
}
