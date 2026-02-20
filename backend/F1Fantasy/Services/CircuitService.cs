using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class CircuitService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly CircuitRepository _circuitRepository;
    private readonly PaginationStateTracker _paginationState;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "circuits";

    public CircuitService(HttpClient httpClient, CircuitRepository circuitRepository, PaginationStateTracker paginationState)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _circuitRepository = circuitRepository;
        _paginationState = paginationState;
    }

    public async Task<IEnumerable<Circuit>> GetAllCircuitsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            Console.WriteLine("Circuits data is complete and fresh. Returning cached data.");
            return _circuitRepository.GetAll();
        }

        var allCircuits = new List<Circuit>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        try
        {
            do
            {
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/circuits/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<CircuitApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.CircuitTable?.Circuits == null)
                {
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");

                // Store circuits in repository
                foreach (var circuit in apiResponse.MRData.CircuitTable.Circuits)
                {
                    allCircuits.Add(circuit);
                    _circuitRepository.AddOrUpdate(circuit);
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
            return _circuitRepository.GetAll();
        }
        catch (HttpRequestException ex)
        {
            // If API fails, the state is already saved at last successful offset
            Console.WriteLine($"API call failed for circuits at offset {offset}. State saved. Error: {ex.Message}");
            var cachedCircuits = _circuitRepository.GetAll().ToList();
            
            // If we have partial data, it's already in repository
            if (cachedCircuits.Any())
            {
                Console.WriteLine($"Returning {cachedCircuits.Count} cached circuits. Will resume from offset {_paginationState.GetNextOffset(StateKey)} on next call.");
            }
            
            return cachedCircuits;
        }
    }

    public async Task<Circuit?> GetCircuitByIdAsync(string circuitId)
    {
        // Check repository first
        var cachedCircuit = _circuitRepository.GetByCircuitId(circuitId);
        if (cachedCircuit != null)
        {
            return cachedCircuit;
        }

        try
        {
            // If not in repository, fetch all circuits (which will populate the repository)
            await GetAllCircuitsAsync();
        }
        catch (HttpRequestException)
        {
            // API failed, but we already checked cache above, so return null
            Console.WriteLine($"API call failed for circuit {circuitId}. No cached data available.");
        }
        
        return _circuitRepository.GetByCircuitId(circuitId);
    }

    public IEnumerable<Circuit> GetCachedCircuits()
    {
        return _circuitRepository.GetAll();
    }
}
