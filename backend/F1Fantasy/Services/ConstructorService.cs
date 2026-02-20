using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class ConstructorService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly ConstructorRepository _constructorRepository;
    private readonly PaginationStateTracker _paginationState;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "constructors";

    public ConstructorService(HttpClient httpClient, ConstructorRepository constructorRepository, PaginationStateTracker paginationState)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _constructorRepository = constructorRepository;
        _paginationState = paginationState;
    }

    public async Task<IEnumerable<Constructor>> GetAllConstructorsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            Console.WriteLine("Constructors data is complete and fresh. Returning cached data.");
            return _constructorRepository.GetAll();
        }

        var allConstructors = new List<Constructor>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        try
        {
            do
            {
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/constructors/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<ConstructorApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.ConstructorTable?.Constructors == null)
                {
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");

                // Store constructors in repository
                foreach (var constructor in apiResponse.MRData.ConstructorTable.Constructors)
                {
                    allConstructors.Add(constructor);
                    _constructorRepository.AddOrUpdate(constructor);
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
            return _constructorRepository.GetAll();
        }
        catch (HttpRequestException ex)
        {
            // If API fails, the state is already saved at last successful offset
            Console.WriteLine($"API call failed for constructors at offset {offset}. State saved. Error: {ex.Message}");
            var cachedConstructors = _constructorRepository.GetAll().ToList();
            
            // If we have partial data, it's already in repository
            if (cachedConstructors.Any())
            {
                Console.WriteLine($"Returning {cachedConstructors.Count} cached constructors. Will resume from offset {_paginationState.GetNextOffset(StateKey)} on next call.");
            }
            
            return cachedConstructors;
        }
    }

    public async Task<IEnumerable<Constructor>> GetConstructorsBySeasonAsync(string season)
    {
        try
        {
            var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/{season}/constructors/");
            var apiResponse = JsonSerializer.Deserialize<ConstructorApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.MRData?.ConstructorTable?.Constructors == null)
            {
                // Fall back to all cached constructors (we don't track by season in repository)
                return _constructorRepository.GetAll();
            }

            var constructors = apiResponse.MRData.ConstructorTable.Constructors;

            // Store in repository
            foreach (var constructor in constructors)
            {
                _constructorRepository.AddOrUpdate(constructor);
            }

            return constructors;
        }
        catch (HttpRequestException)
        {
            // If API fails completely (even after retries), fall back to all cached data
            Console.WriteLine($"API call failed for constructors in season {season}. Returning all cached constructors.");
            return _constructorRepository.GetAll();
        }
    }

    public async Task<Constructor?> GetConstructorByIdAsync(string constructorId)
    {
        // Check repository first
        var cachedConstructor = _constructorRepository.GetByConstructorId(constructorId);
        if (cachedConstructor != null)
        {
            return cachedConstructor;
        }

        try
        {
            // If not in repository, fetch all constructors (which will populate the repository)
            await GetAllConstructorsAsync();
        }
        catch (HttpRequestException)
        {
            // API failed, but we already checked cache above, so return null
            Console.WriteLine($"API call failed for constructor {constructorId}. No cached data available.");
        }
        
        return _constructorRepository.GetByConstructorId(constructorId);
    }

    public IEnumerable<Constructor> GetCachedConstructors()
    {
        return _constructorRepository.GetAll();
    }
}
