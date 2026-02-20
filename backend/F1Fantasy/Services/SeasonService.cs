using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class SeasonService
{
    private readonly ApiHttpClient _apiHttpClient;
    private readonly SeasonRepository _seasonRepository;
    private readonly PaginationStateTracker _paginationState;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";
    private const string StateKey = "seasons";

    public SeasonService(HttpClient httpClient, SeasonRepository seasonRepository, PaginationStateTracker paginationState)
    {
        _apiHttpClient = new ApiHttpClient(httpClient);
        _seasonRepository = seasonRepository;
        _paginationState = paginationState;
    }

    public async Task<IEnumerable<Season>> GetAllSeasonsAsync()
    {
        // Check if we should fetch (incomplete or stale data)
        if (!_paginationState.ShouldFetch(StateKey))
        {
            Console.WriteLine("Seasons data is complete and fresh. Returning cached data.");
            return _seasonRepository.GetAll();
        }

        var allSeasons = new List<Season>();
        int offset = _paginationState.GetNextOffset(StateKey);
        const int limit = 30;
        int total = 0;

        try
        {
            do
            {
                var content = await _apiHttpClient.GetStringWithRetryAsync($"{ApiBaseUrl}/seasons/?offset={offset}");
                var apiResponse = JsonSerializer.Deserialize<SeasonApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.MRData?.SeasonTable?.Seasons == null)
                {
                    break;
                }

                total = int.Parse(apiResponse.MRData.Total ?? "0");

                // Convert SeasonData to Season
                foreach (var seasonData in apiResponse.MRData.SeasonTable.Seasons)
                {
                    var season = new Season
                    {
                        Year = seasonData.Season,
                        Url = seasonData.Url
                    };
                    
                    allSeasons.Add(season);
                    _seasonRepository.AddOrUpdate(season);
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
            return _seasonRepository.GetAll();
        }
        catch (HttpRequestException ex)
        {
            // If API fails, the state is already saved at last successful offset
            Console.WriteLine($"API call failed for seasons at offset {offset}. State saved. Error: {ex.Message}");
            var cachedSeasons = _seasonRepository.GetAll().ToList();
            
            // If we have partial data from before the failure, it's already in repository
            if (cachedSeasons.Any())
            {
                Console.WriteLine($"Returning {cachedSeasons.Count} cached seasons. Will resume from offset {_paginationState.GetNextOffset(StateKey)} on next call.");
            }
            
            return cachedSeasons;
        }
    }

    public async Task<Season?> GetSeasonByYearAsync(string year)
    {
        // Check repository first
        var cachedSeason = _seasonRepository.GetByYear(year);
        if (cachedSeason != null)
        {
            return cachedSeason;
        }

        try
        {
            // If not in repository, fetch all seasons (which will populate the repository)
            await GetAllSeasonsAsync();
        }
        catch (HttpRequestException)
        {
            // API failed, but we already checked cache above, so return null
            Console.WriteLine($"API call failed for season {year}. No cached data available.");
        }
        
        return _seasonRepository.GetByYear(year);
    }

    public IEnumerable<Season> GetCachedSeasons()
    {
        return _seasonRepository.GetAll();
    }
}
