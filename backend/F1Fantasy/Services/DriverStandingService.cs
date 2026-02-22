using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class DriverStandingService
{
    private readonly HttpClient _httpClient;
    private readonly DriverStandingRepository _repository;
    private readonly ILogger<DriverStandingService> _logger;

    public DriverStandingService(
        HttpClient httpClient,
        DriverStandingRepository repository,
        ILogger<DriverStandingService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.jolpi.ca/ergast/f1/");
        _repository = repository;
        _logger = logger;
    }

    public async Task<StandingsList?> GetDriverStandingsBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching driver standings for season {Season}", season);
        
        try
        {
            var url = $"{season}/driverstandings.json";
            _logger.LogDebug("Calling Ergast API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status {StatusCode} for driver standings: {Season}", 
                    response.StatusCode, season);
                
                // Fallback to cached data
                return await BuildStandingsFromCache(season, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var apiResponse = JsonSerializer.Deserialize<DriverStandingApiResponse>(content, options);

            if (apiResponse?.MRData?.StandingsTable?.StandingsLists == null || 
                !apiResponse.MRData.StandingsTable.StandingsLists.Any())
            {
                _logger.LogWarning("No driver standings data found for season {Season}", season);
                return await BuildStandingsFromCache(season, null);
            }

            var standingsList = apiResponse.MRData.StandingsTable.StandingsLists.First();
            if (standingsList.DriverStandings == null || !standingsList.DriverStandings.Any())
            {
                _logger.LogWarning("No driver standings found for season {Season}", season);
                return await BuildStandingsFromCache(season, null);
            }

            _logger.LogDebug("Processing {Count} driver standings for season {Season}, round {Round}", 
                standingsList.DriverStandings.Count, season, standingsList.Round);

            // Store each driver standing in the database
            foreach (var entry in standingsList.DriverStandings)
            {
                if (entry.Driver == null)
                {
                    _logger.LogWarning("Driver standing entry missing driver information");
                    continue;
                }

                var standing = new DriverStanding
                {
                    Season = standingsList.Season,
                    Round = standingsList.Round,
                    Position = entry.Position,
                    PositionText = entry.PositionText,
                    Points = entry.Points,
                    Wins = entry.Wins,
                    DriverId = entry.Driver.DriverId,
                    ConstructorId = entry.Constructors?.FirstOrDefault()?.ConstructorId ?? string.Empty
                };

                await _repository.AddOrUpdateAsync(standing);
            }

            _logger.LogInformation("Successfully stored {Count} driver standings for season {Season}, round {Round}", 
                standingsList.DriverStandings.Count, season, standingsList.Round);

            return await BuildStandingsFromCache(season, standingsList.Round);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching driver standings for season {Season}", season);
            return await BuildStandingsFromCache(season, null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse driver standings API response for season {Season}", season);
            return await BuildStandingsFromCache(season, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching driver standings for season {Season}", season);
            throw;
        }
    }

    public async Task<StandingsList?> GetDriverStandingsByRoundAsync(string season, string round)
    {
        _logger.LogInformation("Fetching driver standings for season {Season}, round {Round}", season, round);
        
        try
        {
            var url = $"{season}/{round}/driverstandings.json";
            _logger.LogDebug("Calling Ergast API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status {StatusCode} for driver standings: {Season}/{Round}", 
                    response.StatusCode, season, round);
                
                // Fallback to cached data
                return await BuildStandingsFromCache(season, round);
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var apiResponse = JsonSerializer.Deserialize<DriverStandingApiResponse>(content, options);

            if (apiResponse?.MRData?.StandingsTable?.StandingsLists == null || 
                !apiResponse.MRData.StandingsTable.StandingsLists.Any())
            {
                _logger.LogWarning("No driver standings data found for season {Season}, round {Round}", season, round);
                return await BuildStandingsFromCache(season, round);
            }

            var standingsList = apiResponse.MRData.StandingsTable.StandingsLists.First();
            if (standingsList.DriverStandings == null || !standingsList.DriverStandings.Any())
            {
                _logger.LogWarning("No driver standings found for season {Season}, round {Round}", season, round);
                return await BuildStandingsFromCache(season, round);
            }

            _logger.LogDebug("Processing {Count} driver standings for season {Season}, round {Round}", 
                standingsList.DriverStandings.Count, season, round);

            // Store each driver standing in the database
            foreach (var entry in standingsList.DriverStandings)
            {
                if (entry.Driver == null)
                {
                    _logger.LogWarning("Driver standing entry missing driver information");
                    continue;
                }

                var standing = new DriverStanding
                {
                    Season = standingsList.Season,
                    Round = standingsList.Round,
                    Position = entry.Position,
                    PositionText = entry.PositionText,
                    Points = entry.Points,
                    Wins = entry.Wins,
                    DriverId = entry.Driver.DriverId,
                    ConstructorId = entry.Constructors?.FirstOrDefault()?.ConstructorId ?? string.Empty
                };

                await _repository.AddOrUpdateAsync(standing);
            }

            _logger.LogInformation("Successfully stored {Count} driver standings for season {Season}, round {Round}", 
                standingsList.DriverStandings.Count, season, round);

            return await BuildStandingsFromCache(season, round);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching driver standings for season {Season}, round {Round}", 
                season, round);
            return await BuildStandingsFromCache(season, round);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse driver standings API response for season {Season}, round {Round}", 
                season, round);
            return await BuildStandingsFromCache(season, round);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching driver standings for season {Season}, round {Round}", 
                season, round);
            throw;
        }
    }

    public async Task<DriverStanding?> GetDriverStandingByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogInformation("Fetching driver standing for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        try
        {
            // First try to get from cache
            var cachedStanding = await _repository.GetByDriverAsync(season, round, driverId);
            if (cachedStanding != null)
            {
                _logger.LogDebug("Found driver standing in cache for driver {DriverId}", driverId);
                return cachedStanding;
            }

            // If not in cache, fetch all standings for the round
            _logger.LogDebug("Driver {DriverId} standing not in cache, fetching from API", driverId);
            await GetDriverStandingsByRoundAsync(season, round);

            // Try cache again
            cachedStanding = await _repository.GetByDriverAsync(season, round, driverId);
            if (cachedStanding == null)
            {
                _logger.LogWarning("No standing found for driver {DriverId} after API fetch", driverId);
                return null;
            }

            return cachedStanding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver standing for season {Season}, round {Round}, driver {DriverId}", 
                season, round, driverId);
            throw;
        }
    }

    public async Task<IEnumerable<DriverStanding>> GetCachedStandingsAsync()
    {
        _logger.LogInformation("Fetching all cached driver standings");
        
        try
        {
            var cachedStandings = await _repository.GetAllAsync();
            _logger.LogDebug("Found {Count} driver standings in cache", cachedStandings.Count());
            return cachedStandings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached driver standings");
            throw;
        }
    }

    private async Task<StandingsList?> BuildStandingsFromCache(string season, string? round)
    {
        _logger.LogDebug("Building driver standings from cache for season {Season}, round {Round}", 
            season, round ?? "latest");
        
        try
        {
            IEnumerable<DriverStanding> cachedStandings;
            
            if (string.IsNullOrEmpty(round))
            {
                // Get latest round standings for the season
                cachedStandings = await _repository.GetBySeasonAsync(season);
            }
            else
            {
                cachedStandings = await _repository.GetBySeasonAndRoundAsync(season, round);
            }
            
            if (!cachedStandings.Any())
            {
                _logger.LogWarning("No cached driver standings found for season {Season}, round {Round}", 
                    season, round ?? "latest");
                return null;
            }

            var firstStanding = cachedStandings.First();
            
            var driverEntries = cachedStandings.Select(s => new DriverStandingEntry
            {
                Position = s.Position,
                PositionText = s.PositionText,
                Points = s.Points,
                Wins = s.Wins,
                Driver = new Driver { DriverId = s.DriverId },
                Constructors = new List<Constructor> 
                { 
                    new Constructor { ConstructorId = s.ConstructorId } 
                }
            }).ToList();

            _logger.LogDebug("Built {Count} driver standings from cache", driverEntries.Count);

            return new StandingsList
            {
                Season = firstStanding.Season,
                Round = firstStanding.Round,
                DriverStandings = driverEntries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building driver standings from cache for season {Season}, round {Round}", 
                season, round ?? "latest");
            return null;
        }
    }
}
