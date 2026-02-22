using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class QualifyingService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly ILogger<QualifyingService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public QualifyingService(
        HttpClient httpClient, 
        QualifyingRepository qualifyingRepository, 
        ILogger<QualifyingService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _qualifyingRepository = qualifyingRepository;
        _logger = logger;
    }

    /// <summary>
    /// Cache-first: Returns cached qualifying data if available, otherwise fetches from API
    /// </summary>
    public async Task<IEnumerable<RaceWithQualifying>> GetQualifyingBySeasonCachedAsync(string season)
    {
        _logger.LogInformation("Checking cache for qualifying results for season {Season}", season);
        
        var cachedData = await BuildQualifyingFromCache(season);
        if (cachedData.Any())
        {
            _logger.LogInformation("Returning cached qualifying data for season {Season} ({Count} races)", season, cachedData.Count());
            return cachedData;
        }

        _logger.LogInformation("No cached qualifying data found for season {Season}, fetching from API", season);
        return await GetQualifyingBySeasonAsync(season);
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

            // Store qualifying results in repository
            foreach (var race in races)
            {
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
            }

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season} qualifying. Returning cached data.", season);
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
