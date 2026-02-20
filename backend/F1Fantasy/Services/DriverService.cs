using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class DriverService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly DriverRepository _driverRepository;
    private readonly PaginationStateTracker _paginationState;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "drivers";

    public DriverService(HttpClient httpClient, DriverRepository driverRepository, PaginationStateTracker paginationState)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _driverRepository = driverRepository;
        _paginationState = paginationState;
    }

    public async Task<IEnumerable<Driver>> GetAllDriversAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            Console.WriteLine("Drivers data is complete and fresh. Returning cached data.");
            return await _driverRepository.GetAllAsync();
        }

        var allDrivers = new List<Driver>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        try
        {
            do
            {
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/drivers/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<DriverApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.DriverTable?.Drivers == null)
                {
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");

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
            }

            // Return all data (including previously cached)
            return await _driverRepository.GetAllAsync();
        }
        catch (HttpRequestException ex)
        {
            // If API fails, the state is already saved at last successful offset
            Console.WriteLine($"API call failed for drivers at offset {offset}. State saved. Error: {ex.Message}");
            var cachedDrivers = (await _driverRepository.GetAllAsync()).ToList();
            
            // If we have partial data, it's already in repository
            if (cachedDrivers.Any())
            {
                Console.WriteLine($"Returning {cachedDrivers.Count} cached drivers. Will resume from offset {_paginationState.GetNextOffset(StateKey)} on next call.");
            }
            
            return cachedDrivers;
        }
    }

    public async Task<IEnumerable<Driver>> GetDriversBySeasonAsync(string season)
    {
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/drivers/");
            var apiResponse = JsonSerializer.Deserialize<DriverApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.DriverTable?.Drivers == null)
            {
                // Fall back to all cached drivers (we don't track by season in repository)
                return await _driverRepository.GetAllAsync();
            }

            var drivers = apiResponse.MRData.DriverTable.Drivers;

            // Store in repository
            foreach (var driver in drivers)
            {
                await _driverRepository.AddOrUpdateAsync(driver);
            }

            return drivers;
        }
        catch (HttpRequestException)
        {
            // If API fails completely (even after retries), fall back to all cached data
            Console.WriteLine($"API call failed for drivers in season {season}. Returning all cached drivers.");
            return await _driverRepository.GetAllAsync();
        }
    }

    public async Task<Driver?> GetDriverByIdAsync(string driverId)
    {
        // Check repository first
        var cachedDriver = await _driverRepository.GetByDriverIdAsync(driverId);
        if (cachedDriver != null)
        {
            return cachedDriver;
        }

        try
        {
            // If not in repository, fetch all drivers (which will populate the repository)
            await GetAllDriversAsync();
        }
        catch (HttpRequestException)
        {
            // API failed, but we already checked cache above, so return null
            Console.WriteLine($"API call failed for driver {driverId}. No cached data available.");
        }
        
        return await _driverRepository.GetByDriverIdAsync(driverId);
    }

    public async Task<IEnumerable<Driver>> GetCachedDrivers()
    {
        return await _driverRepository.GetAllAsync();
    }
}
