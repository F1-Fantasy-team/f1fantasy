using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class CircuitService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly CircuitRepository _circuitRepository;
    private readonly PaginationStateTracker _paginationState;
    private readonly ILogger<CircuitService> _logger;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "circuits";

    public CircuitService(
        HttpClient httpClient, 
        CircuitRepository circuitRepository, 
        PaginationStateTracker paginationState,
        ILogger<CircuitService> logger)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _circuitRepository = circuitRepository;
        _paginationState = paginationState;
        _logger = logger;
    }

    public async Task<IEnumerable<Circuit>> GetAllCircuitsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            _logger.LogInformation("Circuits data is complete and fresh. Returning {Count} cached circuits.", 
                (await _circuitRepository.GetAllAsync()).Count());
            return await _circuitRepository.GetAllAsync();
        }

        var allCircuits = new List<Circuit>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        _logger.LogInformation("Fetching circuits from API starting at offset {Offset}", offset);

        try
        {
            do
            {
                _logger.LogDebug("Fetching circuits batch at offset {Offset}", offset);
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/circuits/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<CircuitApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.CircuitTable?.Circuits == null)
                {
                    _logger.LogWarning("API returned null or invalid response at offset {Offset}", offset);
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");
                _logger.LogDebug("Retrieved {Count} circuits in this batch. Total available: {Total}", 
                    apiResponse.MRData.CircuitTable.Circuits.Count, total);

                // Store circuits in repository
                foreach (var circuit in apiResponse.MRData.CircuitTable.Circuits)
                {
                    allCircuits.Add(circuit);
                    await _circuitRepository.AddOrUpdateAsync(circuit);
                }

                // Update state after successful fetch
                _paginationState.UpdateState(StateKey, offset, total);
                offset += limit;

            } while (offset < total);

            // Mark as complete when we've fetched everything
            if (offset >= total)
            {
                _paginationState.MarkComplete(StateKey);
                _logger.LogInformation("Successfully fetched all {Total} circuits from API", total);
            }

            // Return all data (including previously cached)
            return await _circuitRepository.GetAllAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed for circuits at offset {Offset}. Will resume from this offset on next call.", offset);
            var cachedCircuits = (await _circuitRepository.GetAllAsync()).ToList();
            
            // If we have partial data, it's already in repository
            if (cachedCircuits.Any())
            {
                _logger.LogWarning("Returning {Count} cached circuits. Will resume from offset {Offset} on next call.", 
                    cachedCircuits.Count, _paginationState.GetNextOffset(StateKey));
            }
            else
            {
                _logger.LogError("No cached circuits available and API request failed");
            }
            
            return cachedCircuits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching circuits");
            throw;
        }
    }

    public async Task<Circuit?> GetCircuitByIdAsync(string circuitId)
    {
        _logger.LogDebug("Looking up circuit {CircuitId} in cache", circuitId);
        
        // Check repository first
        var cachedCircuit = await _circuitRepository.GetByCircuitIdAsync(circuitId);
        if (cachedCircuit != null)
        {
            _logger.LogDebug("Circuit {CircuitId} found in cache", circuitId);
            return cachedCircuit;
        }

        _logger.LogInformation("Circuit {CircuitId} not in cache. Fetching all circuits to update cache.", circuitId);
        
        try
        {
            // If not in repository, fetch all circuits (which will populate the repository)
            await GetAllCircuitsAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API call failed while trying to find circuit {CircuitId}. No cached data available.", circuitId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching circuit {CircuitId}", circuitId);
            throw;
        }
        
        var circuit = await _circuitRepository.GetByCircuitIdAsync(circuitId);
        if (circuit == null)
        {
            _logger.LogWarning("Circuit {CircuitId} not found even after fetching all circuits", circuitId);
        }
        
        return circuit;
    }

    public async Task<IEnumerable<Circuit>> GetCachedCircuitsAsync()
    {
        _logger.LogDebug("Retrieving all cached circuits from repository");
        var circuits = await _circuitRepository.GetAllAsync();
        _logger.LogInformation("Retrieved {Count} cached circuits", circuits.Count());
        return circuits;
    }
}
