using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class ConstructorStandingService
{
    private readonly HttpClient _httpClient;
    private readonly ConstructorStandingRepository _repository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly ILogger<ConstructorStandingService> _logger;

    public ConstructorStandingService(
        HttpClient httpClient, 
        ConstructorStandingRepository repository, 
        DataFetchMetadataRepository metadataRepository,
        RaceRepository raceRepository,
        ILogger<ConstructorStandingService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.jolpi.ca/ergast/f1/");
        _repository = repository;
        _metadataRepository = metadataRepository;
        _raceRepository = raceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Smart cache-first: Returns cached constructor standings if still valid, otherwise fetches from API
    /// </summary>
    public async Task<ConstructorStandingsList?> GetConstructorStandingsBySeasonCachedAsync(string season)
    {
        _logger.LogInformation("Checking cache for constructor standings for season {Season}", season);
        
        // Check if we should fetch based on staleness
        var shouldFetch = await ShouldFetchConstructorStandingsAsync(season);
        
        if (!shouldFetch)
        {
            var cachedStandings = await GetCachedStandingsBySeasonAsync(season);
            if (cachedStandings != null && cachedStandings.ConstructorStandings.Any())
            {
                _logger.LogInformation("Returning cached constructor standings for season {Season} ({Count} constructors)", 
                    season, cachedStandings.ConstructorStandings.Count);
                return cachedStandings;
            }
        }

        _logger.LogInformation("Cache stale or missing for season {Season}, fetching from API", season);
        return await GetConstructorStandingsBySeasonAsync(season);
    }
    
    private async Task<bool> ShouldFetchConstructorStandingsAsync(string season)
    {
        // Check metadata for last fetch time
        var currentYear = DateTime.UtcNow.Year;
        var seasonYear = int.Parse(season);
        
        // For past seasons, standings are final - less frequent fetching
        TimeSpan cacheExpiration = seasonYear < currentYear 
            ? TimeSpan.FromDays(7) 
            : TimeSpan.FromHours(1); // Current season - check more frequently
        
        var metadata = await _metadataRepository.GetMetadataAsync(season, "ConstructorStandings");
        
        if (metadata == null || !metadata.FetchSuccessful)
        {
            _logger.LogDebug("No valid metadata for ConstructorStandings/{Season}, should fetch", season);
            return true;
        }
        
        var age = DateTime.UtcNow - metadata.LastFetchedAt;
        if (age > cacheExpiration)
        {
            _logger.LogDebug("Constructor standings cache expired for season {Season} (age: {Age}), should fetch", season, age);
            return true;
        }
        
        // Check if there might be a new race since last fetch
        var races = await _raceRepository.GetBySeasonAsync(season);
        var racesSinceLastFetch = races
            .Where(r => DateTime.TryParse(r.Date, out var raceDate) && 
                       raceDate > metadata.LastFetchedAt &&
                       raceDate < DateTime.UtcNow.AddDays(1)) // Race is in the past (with 1 day buffer)
            .ToList();
        
        if (racesSinceLastFetch.Any())
        {
            _logger.LogInformation("Found {Count} race(s) since last fetch for season {Season}, should fetch constructor standings", 
                racesSinceLastFetch.Count, season);
            return true;
        }
        
        _logger.LogDebug("Constructor standings cache valid for season {Season}, skip fetch", season);
        return false;
    }

    public async Task<ConstructorStandingsList?> GetConstructorStandingsBySeasonAsync(string season)
    {
        try
        {
            var url = $"{season}/constructorstandings.json";
            _logger.LogInformation("Fetching constructor standings for season {Season} from {Url}", season, url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API request failed with status {StatusCode}. Falling back to cached data.", response.StatusCode);
                return await GetCachedStandingsBySeasonAsync(season);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var apiResponse = JsonSerializer.Deserialize<ConstructorStandingApiResponse>(content, options);
            
            if (apiResponse?.MRData?.StandingsTable?.StandingsLists == null || !apiResponse.MRData.StandingsTable.StandingsLists.Any())
            {
                _logger.LogWarning("No constructor standings found in API response. Falling back to cached data.");
                return await GetCachedStandingsBySeasonAsync(season);
            }

            var standingsList = apiResponse.MRData.StandingsTable.StandingsLists.First();
            
            // Store in database
            foreach (var standingEntry in standingsList.ConstructorStandings)
            {
                var standing = new ConstructorStanding
                {
                    Season = standingsList.Season,
                    ConstructorId = standingEntry.Constructor.ConstructorId,
                    Round = standingsList.Round,
                    Position = standingEntry.Position,
                    PositionText = standingEntry.PositionText,
                    Points = standingEntry.Points,
                    Wins = standingEntry.Wins
                };
                
                await _repository.AddOrUpdateAsync(standing);
            }

            _logger.LogInformation("Successfully fetched and stored {Count} constructor standings for season {Season}", 
                standingsList.ConstructorStandings.Count, season);

            // Record fetch metadata
            var roundNumber = int.TryParse(standingsList.Round, out var r) ? r : (int?)null;
            await _metadataRepository.RecordFetchAsync(season, "ConstructorStandings", roundNumber, true);

            return standingsList;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching constructor standings for season {Season}. Falling back to cached data.", season);
            await _metadataRepository.RecordFetchAsync(season, "ConstructorStandings", null, false, ex.Message);
            return await GetCachedStandingsBySeasonAsync(season);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for constructor standings season {Season}. Falling back to cached data.", season);
            await _metadataRepository.RecordFetchAsync(season, "ConstructorStandings", null, false, ex.Message);
            return await GetCachedStandingsBySeasonAsync(season);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching constructor standings for season {Season}", season);
            throw;
        }
    }

    public async Task<ConstructorStandingsList?> GetConstructorStandingsByRoundAsync(string season, string round)
    {
        try
        {
            var url = $"{season}/{round}/constructorstandings.json";
            _logger.LogInformation("Fetching constructor standings for season {Season} round {Round} from {Url}", season, round, url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API request failed with status {StatusCode}. Falling back to cached data.", response.StatusCode);
                return await GetCachedStandingsByRoundAsync(season, round);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var apiResponse = JsonSerializer.Deserialize<ConstructorStandingApiResponse>(content, options);
            
            if (apiResponse?.MRData?.StandingsTable?.StandingsLists == null || !apiResponse.MRData.StandingsTable.StandingsLists.Any())
            {
                _logger.LogWarning("No constructor standings found in API response. Falling back to cached data.");
                return await GetCachedStandingsByRoundAsync(season, round);
            }

            var standingsList = apiResponse.MRData.StandingsTable.StandingsLists.First();
            
            // Store in database
            foreach (var standingEntry in standingsList.ConstructorStandings)
            {
                var standing = new ConstructorStanding
                {
                    Season = standingsList.Season,
                    ConstructorId = standingEntry.Constructor.ConstructorId,
                    Round = standingsList.Round,
                    Position = standingEntry.Position,
                    PositionText = standingEntry.PositionText,
                    Points = standingEntry.Points,
                    Wins = standingEntry.Wins
                };
                
                await _repository.AddOrUpdateAsync(standing);
            }

            _logger.LogInformation("Successfully fetched and stored {Count} constructor standings for season {Season} round {Round}", 
                standingsList.ConstructorStandings.Count, season, round);

            return standingsList;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching constructor standings for season {Season} round {Round}. Falling back to cached data.", season, round);
            return await GetCachedStandingsByRoundAsync(season, round);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for constructor standings season {Season} round {Round}. Falling back to cached data.", season, round);
            return await GetCachedStandingsByRoundAsync(season, round);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching constructor standings for season {Season} round {Round}", season, round);
            throw;
        }
    }

    public async Task<ConstructorStandingEntry?> GetConstructorStandingByConstructorAsync(string season, string round, string constructorId)
    {
        try
        {
            // Try to get from cache first
            var cachedStanding = await _repository.GetByConstructorAsync(season, round, constructorId);
            if (cachedStanding != null)
            {
                _logger.LogDebug("Found constructor standing in cache for {ConstructorId} season {Season} round {Round}", constructorId, season, round);
                return new ConstructorStandingEntry
                {
                    Position = cachedStanding.Position,
                    PositionText = cachedStanding.PositionText,
                    Points = cachedStanding.Points,
                    Wins = cachedStanding.Wins,
                    Constructor = new Constructor { ConstructorId = cachedStanding.ConstructorId }
                };
            }

            // If not in cache, fetch from API
            var standingsList = await GetConstructorStandingsByRoundAsync(season, round);
            if (standingsList == null)
                return null;

            return standingsList.ConstructorStandings.FirstOrDefault(cs => cs.Constructor.ConstructorId == constructorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching constructor standing for {ConstructorId} season {Season} round {Round}", constructorId, season, round);
            throw;
        }
    }

    public async Task<List<ConstructorStandingsList>> GetCachedStandingsAsync()
    {
        try
        {
            var allStandings = await _repository.GetAllAsync();
            
            if (!allStandings.Any())
            {
                _logger.LogInformation("No cached constructor standings found");
                return new List<ConstructorStandingsList>();
            }

            // Group by season and round
            var groupedStandings = allStandings
                .GroupBy(cs => new { cs.Season, cs.Round })
                .Select(g => BuildStandingsFromCache(g.Key.Season, g.Key.Round, g.ToList()))
                .ToList();

            _logger.LogInformation("Retrieved {Count} cached constructor standings lists", groupedStandings.Count);
            return groupedStandings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached constructor standings");
            throw;
        }
    }

    private async Task<ConstructorStandingsList?> GetCachedStandingsBySeasonAsync(string season)
    {
        var standings = await _repository.GetBySeasonAsync(season);
        if (!standings.Any())
            return null;

        return BuildStandingsFromCache(season, standings.First().Round, standings);
    }

    private async Task<ConstructorStandingsList?> GetCachedStandingsByRoundAsync(string season, string round)
    {
        var standings = await _repository.GetBySeasonAndRoundAsync(season, round);
        if (!standings.Any())
            return null;

        return BuildStandingsFromCache(season, round, standings);
    }

    private ConstructorStandingsList BuildStandingsFromCache(string season, string round, List<ConstructorStanding> standings)
    {
        return new ConstructorStandingsList
        {
            Season = season,
            Round = round,
            ConstructorStandings = standings.Select(cs => new ConstructorStandingEntry
            {
                Position = cs.Position,
                PositionText = cs.PositionText,
                Points = cs.Points,
                Wins = cs.Wins,
                Constructor = new Constructor { ConstructorId = cs.ConstructorId }
            }).ToList()
        };
    }
}
