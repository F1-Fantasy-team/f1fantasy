using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class RaceService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly RaceRepository _raceRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly CacheStalenessService _cacheStalenessService;
    private readonly ILogger<RaceService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public RaceService(
        HttpClient httpClient, 
        RaceRepository raceRepository, 
        DataFetchMetadataRepository metadataRepository,
        CacheStalenessService cacheStalenessService,
        ILogger<RaceService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _raceRepository = raceRepository;
        _metadataRepository = metadataRepository;
        _cacheStalenessService = cacheStalenessService;
        _logger = logger;
    }

    public async Task<IEnumerable<Race>> GetRacesForSeasonAsync(string season)
    {
        // Check if we have cached data
        var cachedRaces = await _raceRepository.GetBySeasonAsync(season);
        
        // Determine cache expiration based on season
        var currentYear = DateTime.UtcNow.Year;
        var seasonYear = int.Parse(season);
        
        // Use custom options for races since future seasons need special handling
        var raceOptions = new CacheStalenessOptions
        {
            CurrentSeasonExpiration = TimeSpan.FromHours(6), // Current season - races might be added
            PastSeasonExpiration = TimeSpan.FromDays(7),
            CheckRaceSchedule = false, // Races don't depend on race schedule (they ARE the schedule)
            RaceDataAvailabilityBuffer = TimeSpan.Zero
        };
        
        // Override expiration for future seasons
        if (seasonYear > currentYear)
        {
            raceOptions.CurrentSeasonExpiration = TimeSpan.FromHours(24);
        }
        
        // Check if we should fetch from API based on cache age
        var shouldFetch = await _cacheStalenessService.ShouldFetchAsync(season, DataType.Races, raceOptions);
        
        if (!shouldFetch && cachedRaces.Any())
        {
            _logger.LogInformation("Using cached races for season {Season} ({Count} races)", season, cachedRaces.Count());
            return cachedRaces;
        }
        
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
                await _metadataRepository.RecordFetchAsync(season, "Races", null, false, "API returned null");
                // Fall back to cached data if API returns unexpected response
                return cachedRaces;
            }

            var races = apiResponse.MRData.RaceTable.Races;
            _logger.LogInformation("Retrieved {Count} races for season {Season} from API", races.Count, season);
            
            // Store in repository
            foreach (var race in races)
            {
                await _raceRepository.AddOrUpdateAsync(race);
            }
            
            // Record successful fetch
            await _metadataRepository.RecordFetchAsync(season, "Races", races.Count, true);

            return races;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for season {Season}. Returning cached data.", season);
            await _metadataRepository.RecordFetchAsync(season, "Races", null, false, ex.Message);
            // If API fails completely (even after retries), fall back to cached data
            return cachedRaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching races for season {Season}", season);
            await _metadataRepository.RecordFetchAsync(season, "Races", null, false, ex.Message);
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