using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class SeasonService
{
    private readonly HttpClient _httpClient;
    private readonly SeasonRepository _seasonRepository;
    private const string ApiBaseUrl = "https://api.jolpi.ca/ergast/f1";

    public SeasonService(HttpClient httpClient, SeasonRepository seasonRepository)
    {
        _httpClient = httpClient;
        _seasonRepository = seasonRepository;
    }

    public async Task<IEnumerable<Season>> GetAllSeasonsAsync()
    {
        var allSeasons = new List<Season>();
        int offset = 0;
        const int limit = 30;
        int total;

        do
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/seasons/?offset={offset}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
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

            offset += limit;

        } while (offset < total);

        return allSeasons;
    }

    public async Task<Season?> GetSeasonByYearAsync(string year)
    {
        // Check repository first
        var cachedSeason = _seasonRepository.GetByYear(year);
        if (cachedSeason != null)
        {
            return cachedSeason;
        }

        // If not in repository, fetch all seasons (which will populate the repository)
        await GetAllSeasonsAsync();
        
        return _seasonRepository.GetByYear(year);
    }

    public IEnumerable<Season> GetCachedSeasons()
    {
        return _seasonRepository.GetAll();
    }
}
