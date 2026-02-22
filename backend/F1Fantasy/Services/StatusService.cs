using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class StatusService
{
    private readonly HttpClient _httpClient;
    private readonly StatusRepository _repository;
    private readonly ILogger<StatusService> _logger;

    public StatusService(HttpClient httpClient, StatusRepository repository, ILogger<StatusService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.jolpi.ca/ergast/f1/");
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<Status>> GetAllStatusesAsync()
    {
        try
        {
            // Check cache first
            var cachedStatuses = await _repository.GetAllAsync();
            if (cachedStatuses.Any())
            {
                _logger.LogDebug("Returning {Count} cached statuses", cachedStatuses.Count);
                return cachedStatuses;
            }

            // Fetch from API if cache is empty
            _logger.LogInformation("Fetching all statuses from API");
            return await FetchAndCacheStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            throw;
        }
    }

    public async Task<List<Status>> RefreshStatusesAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing statuses from API");
            return await FetchAndCacheStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing statuses");
            throw;
        }
    }

    public async Task<Status?> GetByIdAsync(string statusId)
    {
        try
        {
            // Try cache first
            var status = await _repository.GetByIdAsync(statusId);
            if (status != null)
            {
                _logger.LogDebug("Found status in cache: {StatusId}", statusId);
                return status;
            }

            // If not in cache, fetch all and try again
            _logger.LogDebug("Status {StatusId} not in cache, fetching all statuses", statusId);
            await FetchAndCacheStatusesAsync();
            return await _repository.GetByIdAsync(statusId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status by ID: {StatusId}", statusId);
            throw;
        }
    }

    public async Task<Status?> GetByTextAsync(string statusText)
    {
        try
        {
            // Try cache first
            var status = await _repository.GetByTextAsync(statusText);
            if (status != null)
            {
                _logger.LogDebug("Found status in cache: {StatusText}", statusText);
                return status;
            }

            // If not in cache, fetch all and try again
            _logger.LogDebug("Status '{StatusText}' not in cache, fetching all statuses", statusText);
            await FetchAndCacheStatusesAsync();
            return await _repository.GetByTextAsync(statusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status by text: {StatusText}", statusText);
            throw;
        }
    }

    private async Task<List<Status>> FetchAndCacheStatusesAsync()
    {
        // The API requires pagination, fetch all 136 statuses
        var url = "status.json?limit=1000"; // High limit to get all in one request
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("API request failed with status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"API returned status {response.StatusCode}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var apiResponse = JsonSerializer.Deserialize<StatusApiResponse>(content, options);
        
        if (apiResponse?.MRData?.StatusTable?.Status == null || !apiResponse.MRData.StatusTable.Status.Any())
        {
            _logger.LogWarning("No statuses found in API response");
            return new List<Status>();
        }

        var statuses = apiResponse.MRData.StatusTable.Status;
        _logger.LogInformation("Fetched {Count} statuses from API", statuses.Count);

        // Store all statuses in database
        foreach (var statusEntry in statuses)
        {
            var status = new Status
            {
                StatusId = statusEntry.StatusId,
                StatusText = statusEntry.StatusText,
                Count = statusEntry.Count
            };
            
            await _repository.AddOrUpdateAsync(status);
        }

        _logger.LogInformation("Successfully cached {Count} statuses", statuses.Count);
        
        // Return from cache to get sorted list
        return await _repository.GetAllAsync();
    }
}
