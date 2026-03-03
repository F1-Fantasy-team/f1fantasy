using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class DriverService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly DriverRepository _driverRepository;
    private readonly PaginationStateTracker _paginationState;
    private readonly ILogger<DriverService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "drivers";

    public DriverService(
        HttpClient httpClient, 
        DriverRepository driverRepository, 
        PaginationStateTracker paginationState,
        ILogger<DriverService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _driverRepository = driverRepository;
        _paginationState = paginationState;
        _logger = logger;
    }

    public async Task<IEnumerable<Driver>> GetAllDriversAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            _logger.LogInformation("Drivers data is complete and fresh. Returning {Count} cached drivers.", 
                (await _driverRepository.GetAllAsync()).Count());
            return await _driverRepository.GetAllAsync();
        }

        var allDrivers = new List<Driver>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        _logger.LogInformation("Fetching drivers from API starting at offset {Offset}", offset);

        try
        {
            do
            {
                _logger.LogDebug("Fetching drivers batch at offset {Offset}", offset);
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/drivers/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<DriverApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.DriverTable?.Drivers == null)
                {
                    _logger.LogWarning("API returned null or invalid response at offset {Offset}", offset);
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");
                _logger.LogDebug("Retrieved {Count} drivers in this batch. Total available: {Total}", 
                    apiResponse.MRData.DriverTable.Drivers.Count, total);

                // Store drivers in repository
                foreach (var driver in apiResponse.MRData.DriverTable.Drivers)
                {
                    allDrivers.Add(driver);
                    await _driverRepository.AddOrUpdateAsync(driver);
                }

                // Update state after successful fetch
                _paginationState.UpdateState(StateKey, offset, total);
                offset += limit;

            } while (offset < total);

            // Mark as complete when we've fetched everything
            if (offset >= total)
            {
                _paginationState.MarkComplete(StateKey);
                _logger.LogInformation("Successfully fetched all {Total} drivers from API", total);
            }

            // Return all data (including previously cached)
            return await _driverRepository.GetAllAsync();
        }
        catch (HttpRequestException ex)
        {
            // If API fails, the state is already saved at last successful offset
            _logger.LogError(ex, "API call failed for drivers at offset {Offset}. Will resume from this offset on next call.", offset);
            var cachedDrivers = (await _driverRepository.GetAllAsync()).ToList();
            
            // If we have partial data, it's already in repository
            if (cachedDrivers.Any())
            {
                _logger.LogWarning("Returning {Count} cached drivers. Will resume from offset {Offset} on next call.", 
                    cachedDrivers.Count, _paginationState.GetNextOffset(StateKey));
            }
            else
            {
                _logger.LogError("No cached drivers available and API request failed");
            }
            
            return cachedDrivers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching drivers");
            throw;
        }
    }

    public async Task<IEnumerable<Driver>> GetDriversBySeasonAsync(string season)
    {
        _logger.LogInformation("Fetching drivers for season {Season} from API", season);
        
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/drivers/");
            var apiResponse = JsonSerializer.Deserialize<DriverApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.DriverTable?.Drivers == null)
            {
                _logger.LogWarning("API returned null response for season {Season}. Falling back to cached data.", season);
                // Fall back to all cached drivers (we don't track by season in repository)
                return await _driverRepository.GetAllAsync();
            }

            var drivers = apiResponse.MRData.DriverTable.Drivers;
            _logger.LogInformation("Retrieved {Count} drivers for season {Season} from API", drivers.Count, season);

            // Add this season to ActiveSeasons for all drivers
            foreach (var driver in drivers)
            {
                if (!driver.ActiveSeasons.Contains(season))
                {
                    driver.ActiveSeasons.Add(season);
                }
            }
            
            // Batch save all drivers in one operation
            await _driverRepository.AddOrUpdateBatchAsync(drivers);

            return drivers;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for drivers in season {Season}. Returning all cached drivers.", season);
            // If API fails completely (even after retries), fall back to all cached data
            return await _driverRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching drivers for season {Season}", season);
            throw;
        }
    }

    public async Task<Driver?> GetDriverByIdAsync(string driverId)
    {
        _logger.LogDebug("Looking up driver {DriverId} in cache", driverId);
        
        // Check repository first
        var cachedDriver = await _driverRepository.GetByDriverIdAsync(driverId);
        if (cachedDriver != null)
        {
            _logger.LogDebug("Driver {DriverId} found in cache", driverId);
            return cachedDriver;
        }

        _logger.LogInformation("Driver {DriverId} not in cache. Fetching all drivers to update cache.", driverId);
        
        try
        {
            // If not in repository, fetch all drivers (which will populate the repository)
            await GetAllDriversAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed while trying to find driver {DriverId}. No cached data available.", driverId);
            // API failed, but we already checked cache above, so return null
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching driver {DriverId}", driverId);
            throw;
        }
        
        var driver = await _driverRepository.GetByDriverIdAsync(driverId);
        if (driver == null)
        {
            _logger.LogWarning("Driver {DriverId} not found even after fetching all drivers", driverId);
        }
        
        return driver;
    }

    public async Task<IEnumerable<Driver>> GetCachedDriversAsync()
    {
        _logger.LogDebug("Retrieving all cached drivers from repository");
        var drivers = await _driverRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached drivers", drivers.Count());
        return drivers;
    }

    public async Task<IEnumerable<Driver>> GetActiveDriversAsync(string? season = null)
    {
        // Use current year if season not specified
        season ??= DateTime.UtcNow.Year.ToString();
        
        _logger.LogInformation("[GetActiveDriversAsync] Getting active drivers for season {Season}", season);
        
        // Check if we have any active drivers for this season
        var activeDrivers = await _driverRepository.GetActiveDriversAsync(season);
        _logger.LogInformation("[GetActiveDriversAsync] Initial query returned {Count} drivers for season {Season}", 
            activeDrivers.Count(), season);
        
        // If no active drivers found, fetch them from the API
        if (!activeDrivers.Any())
        {
            _logger.LogWarning("[GetActiveDriversAsync] No active drivers found for season {Season}. Attempting to fetch from API...", season);
            try
            {
                // This will populate the ActiveSeasons list
                var fetchedDrivers = await GetDriversBySeasonAsync(season);
                _logger.LogInformation("[GetActiveDriversAsync] GetDriversBySeasonAsync returned {Count} drivers", fetchedDrivers.Count());
                
                // Retrieve again after populating
                activeDrivers = await _driverRepository.GetActiveDriversAsync(season);
                _logger.LogInformation("[GetActiveDriversAsync] Successfully populated {Count} active drivers for season {Season}", 
                    activeDrivers.Count(), season);
                
                if (!activeDrivers.Any())
                {
                    _logger.LogError("[GetActiveDriversAsync] After API fetch, still no active drivers found for season {Season}. This indicates a data population issue.", season);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GetActiveDriversAsync] Failed to fetch active drivers for season {Season} from API: {Message}", season, ex.Message);
                throw;
            }
        }
        else
        {
            _logger.LogInformation("[GetActiveDriversAsync] Retrieved {Count} active drivers for season {Season} from database", 
                activeDrivers.Count(), season);
        }
        
        return activeDrivers;
    }
}
