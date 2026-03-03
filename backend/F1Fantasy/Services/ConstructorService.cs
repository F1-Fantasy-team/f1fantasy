using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class ConstructorService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly ConstructorRepository _constructorRepository;
    private readonly PaginationStateTracker _paginationState;
    private readonly ILogger<ConstructorService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "constructors";

    public ConstructorService(
        HttpClient httpClient, 
        ConstructorRepository constructorRepository, 
        PaginationStateTracker paginationState,
        ILogger<ConstructorService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _constructorRepository = constructorRepository;
        _paginationState = paginationState;
        _logger = logger;
    }

    public async Task<IEnumerable<Constructor>> GetAllConstructorsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            _logger.LogInformation("Constructors data is complete and fresh. Returning {Count} cached constructors.", 
                (await _constructorRepository.GetAllAsync()).Count());
            return await _constructorRepository.GetAllAsync();
        }

        var allConstructors = new List<Constructor>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        _logger.LogInformation("Fetching constructors from API starting at offset {Offset}", offset);

        try
        {
            do
            {
                _logger.LogDebug("Fetching constructors batch at offset {Offset}", offset);
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/constructors/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<ConstructorApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.ConstructorTable?.Constructors == null)
                {
                    _logger.LogWarning("API returned null or invalid response at offset {Offset}", offset);
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");
                _logger.LogDebug("Retrieved {Count} constructors in this batch. Total available: {Total}", 
                    apiResponse.MRData.ConstructorTable.Constructors.Count, total);

                // Store constructors in repository
                foreach (var constructor in apiResponse.MRData.ConstructorTable.Constructors)
                {
                    allConstructors.Add(constructor);
                    await _constructorRepository.AddOrUpdateAsync(constructor);
                }

                // Update state after successful fetch
                _paginationState.UpdateState(StateKey, offset, total);
                offset += limit;

            } while (offset < total);

            // Mark as complete when we've fetched everything
            if (offset >= total)
            {
                _paginationState.MarkComplete(StateKey);
                _logger.LogInformation("Successfully fetched all {Total} constructors from API", total);
            }

            // Return all data (including previously cached)
            return await _constructorRepository.GetAllAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for constructors at offset {Offset}. Will resume from this offset on next call.", offset);
            var cachedConstructors = (await _constructorRepository.GetAllAsync()).ToList();
            
            // If we have partial data, it's already in repository
            if (cachedConstructors.Any())
            {
                _logger.LogWarning("Returning {Count} cached constructors. Will resume from offset {Offset} on next call.", 
                    cachedConstructors.Count, _paginationState.GetNextOffset(StateKey));
            }
            else
            {
                _logger.LogError("No cached constructors available and API request failed");
            }
            
            return cachedConstructors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching constructors");
            throw;
        }
    }

    public async Task<IEnumerable<Constructor>> GetConstructorsBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching constructors for season {Season} from API", season);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/constructors/");
            var apiResponse = JsonSerializer.Deserialize<ConstructorApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.ConstructorTable?.Constructors == null)
            {
                _logger.LogWarning("API returned null response for season {Season}. Falling back to cached data.", season);
                return await _constructorRepository.GetAllAsync();
            }

            var constructors = apiResponse.MRData.ConstructorTable.Constructors;
            _logger.LogInformation("Retrieved {Count} constructors for season {Season} from API", constructors.Count, season);

            // Add this season to ActiveSeasons for all constructors
            foreach (var constructor in constructors)
            {
                if (!constructor.ActiveSeasons.Contains(season))
                {
                    constructor.ActiveSeasons.Add(season);
                }
            }
            
            // Batch save all constructors in one operation
            await _constructorRepository.AddOrUpdateBatchAsync(constructors);

            return constructors;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for constructors in season {Season}. Returning all cached constructors.", season);
            return await _constructorRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching constructors for season {Season}", season);
            throw;
        }
    }

    public async Task<Constructor?> GetConstructorByIdAsync(string constructorId)
    {
        _logger.LogDebug("Looking up constructor {ConstructorId} in cache", constructorId);
        
        // Check repository first
        var cachedConstructor = await _constructorRepository.GetByConstructorIdAsync(constructorId);
        if (cachedConstructor != null)
        {
            _logger.LogDebug("Constructor {ConstructorId} found in cache", constructorId);
            return cachedConstructor;
        }

        _logger.LogInformation("Constructor {ConstructorId} not in cache. Fetching all constructors to update cache.", constructorId);
        
        try
        {
            // If not in repository, fetch all constructors (which will populate the repository)
            await GetAllConstructorsAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed while trying to find constructor {ConstructorId}. No cached data available.", constructorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching constructor {ConstructorId}", constructorId);
            throw;
        }
        
        var constructor = await _constructorRepository.GetByConstructorIdAsync(constructorId);
        if (constructor == null)
        {
            _logger.LogWarning("Constructor {ConstructorId} not found even after fetching all constructors", constructorId);
        }
        
        return constructor;
    }

    public async Task<IEnumerable<Constructor>> GetCachedConstructorsAsync()
    {
        _logger.LogDebug("Retrieving all cached constructors from repository");
        var constructors = await _constructorRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached constructors", constructors.Count());
        return constructors;
    }

    public async Task<IEnumerable<Constructor>> GetActiveConstructorsAsync(string? season = null)
    {
        // Use current year if season not specified
        season ??= DateTime.UtcNow.Year.ToString();
        
        _logger.LogInformation("[GetActiveConstructorsAsync] Getting active constructors for season {Season}", season);
        
        // Check if we have any active constructors for this season
        var activeConstructors = await _constructorRepository.GetActiveConstructorsAsync(season);
        _logger.LogInformation("[GetActiveConstructorsAsync] Initial query returned {Count} constructors for season {Season}", 
            activeConstructors.Count(), season);
        
        // If no active constructors found, fetch them from the API
        if (!activeConstructors.Any())
        {
            _logger.LogWarning("[GetActiveConstructorsAsync] No active constructors found for season {Season}. Attempting to fetch from API...", season);
            try
            {
                // This will populate the ActiveSeasons list
                var fetchedConstructors = await GetConstructorsBySeasonAsync(season);
                _logger.LogInformation("[GetActiveConstructorsAsync] GetConstructorsBySeasonAsync returned {Count} constructors", fetchedConstructors.Count());
                
                // Retrieve again after populating
                activeConstructors = await _constructorRepository.GetActiveConstructorsAsync(season);
                _logger.LogInformation("[GetActiveConstructorsAsync] Successfully populated {Count} active constructors for season {Season}", 
                    activeConstructors.Count(), season);
                
                if (!activeConstructors.Any())
                {
                    _logger.LogError("[GetActiveConstructorsAsync] After API fetch, still no active constructors found for season {Season}. This indicates a data population issue.", season);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetActiveConstructorsAsync] Failed to fetch active constructors for season {Season} from API: {Message}", season, ex.Message);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("[GetActiveConstructorsAsync] Retrieved {Count} active constructors for season {Season} from database", 
                activeConstructors.Count(), season);
        }
        
        return activeConstructors;
    }
}
