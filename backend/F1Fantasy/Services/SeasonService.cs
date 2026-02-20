using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class SeasonService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly SeasonRepository _seasonRepository;
    private readonly PaginationStateTracker _paginationState;
    private readonly ILogger<SeasonService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "seasons";

    public SeasonService(
        HttpClient httpClient, 
        SeasonRepository seasonRepository, 
        PaginationStateTracker paginationState,
        ILogger<SeasonService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _seasonRepository = seasonRepository;
        _paginationState = paginationState;
        _logger = logger;
    }

    public async Task<IEnumerable<Season>> GetAllSeasonsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            _logger.LogInformation("Seasons data is complete and fresh. Returning {Count} cached seasons.", 
                (await _seasonRepository.GetAllAsync()).Count());
            return await _seasonRepository.GetAllAsync();
        }

        var allSeasons = new List<Season>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        _logger.LogInformation("Fetching seasons from API starting at offset {Offset}", offset);

        try
        {
            do
            {
                _logger.LogDebug("Fetching seasons batch at offset {Offset}", offset);
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/seasons/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<SeasonApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.SeasonTable?.Seasons == null)
                {
                    _logger.LogWarning("API returned null or invalid response at offset {Offset}", offset);
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");
                _logger.LogDebug("Retrieved {Count} seasons in this batch. Total available: {Total}", 
                    apiResponse.MRData.SeasonTable.Seasons.Count, total);

                // Convert SeasonData to Season
                foreach (var seasonData in apiResponse.MRData.SeasonTable.Seasons)
                {
                    var season = new Season
                    {
                        Year = seasonData.Season,
                        Url = seasonData.Url
                    };
                    
                    allSeasons.Add(season);
                    await _seasonRepository.AddOrUpdateAsync(season);
                }

                // Update state after successful fetch
                _paginationState.UpdateState(StateKey, offset, total);
                offset += limit;

            } while (offset < total);

            // Mark as complete when we've fetched everything
            if (offset >= total)
            {
                _paginationState.MarkComplete(StateKey);
                _logger.LogInformation("Successfully fetched all {Total} seasons from API", total);
            }

            // Return all data (including previously cached)
            return await _seasonRepository.GetAllAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for seasons at offset {Offset}. Will resume from this offset on next call.", offset);
            var cachedSeasons = (await _seasonRepository.GetAllAsync()).ToList();
            
            // If we have partial data from before the failure, it's already in repository
            if (cachedSeasons.Any())
            {
                _logger.LogWarning("Returning {Count} cached seasons. Will resume from offset {Offset} on next call.", 
                    cachedSeasons.Count, _paginationState.GetNextOffset(StateKey));
            }
            else
            {
                _logger.LogError("No cached seasons available and API request failed");
            }
            
            return cachedSeasons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching seasons");
            throw;
        }
    }

    public async Task<Season?> GetSeasonByYearAsync(string year)
    {
        _logger.LogDebug("Looking up season {Year} in cache", year);
        
        // Check repository first
        var cachedSeason = await _seasonRepository.GetByYearAsync(year);
        if (cachedSeason != null)
        {
            _logger.LogDebug("Season {Year} found in cache", year);
            return cachedSeason;
        }

        _logger.LogInformation("Season {Year} not in cache. Fetching all seasons to update cache.", year);
        
        try
        {
            // If not in repository, fetch all seasons (which will populate the repository)
            await GetAllSeasonsAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed while trying to find season {Year}. No cached data available.", year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching season {Year}", year);
            throw;
        }
        
        var season = await _seasonRepository.GetByYearAsync(year);
        if (season == null)
        {
            _logger.LogWarning("Season {Year} not found even after fetching all seasons", year);
        }
        
        return season;
    }

    public async Task<IEnumerable<Season>> GetCachedSeasonsAsync()
    {
        _logger.LogDebug("Retrieving all cached seasons from repository");
        var seasons = await _seasonRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached seasons", seasons.Count());
        return seasons;
    }
}
