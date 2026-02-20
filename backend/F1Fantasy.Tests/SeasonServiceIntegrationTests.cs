using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for SeasonService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can actually fetch and parse season data from the real API
/// and handle pagination correctly
/// </summary>
public class SeasonServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SeasonRepository _seasonRepository;
    private readonly SeasonService _seasonService;

    public SeasonServiceIntegrationTests()
    {
        _httpClient = new HttpClient();
        _seasonRepository = new SeasonRepository();
        _seasonService = new SeasonService(_httpClient, _seasonRepository);
    }

    [Fact]
    public async Task GetAllSeasonsAsync_FetchesAndParsesAllSeasons()
    {
        // Arrange & Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert
        seasons.Should().NotBeNull();
        seasons.Should().NotBeEmpty("F1 has been running since 1950");
        
        var seasonList = seasons.ToList();
        seasonList.Should().HaveCountGreaterThan(70, "F1 has had over 70 seasons since 1950");

        // Verify first season (1950)
        var firstSeason = seasonList.First();
        firstSeason.Year.Should().Be("1950", "F1 started in 1950");
        firstSeason.Url.Should().NotBeNullOrEmpty();
        firstSeason.Url.Should().Contain("wikipedia", "season URLs point to Wikipedia");
    }

    [Fact]
    public async Task GetAllSeasonsAsync_HandlesPaginationCorrectly()
    {
        // Arrange
        _seasonRepository.Clear();

        // Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert
        var seasonList = seasons.ToList();
        
        // API returns 30 items per page, total should be > 30 to verify pagination worked
        seasonList.Should().HaveCountGreaterThan(30, "pagination should fetch more than one page");
        
        // Verify we have consecutive years (no gaps from pagination issues)
        var years = seasonList.Select(s => int.Parse(s.Year)).OrderBy(y => y).ToList();
        years.First().Should().Be(1950);
        
        // Check that years are consecutive
        for (int i = 0; i < years.Count - 1; i++)
        {
            var gap = years[i + 1] - years[i];
            gap.Should().BeLessThanOrEqualTo(1, $"years should be consecutive or have 1 year gap, found {years[i]} -> {years[i + 1]}");
        }
    }

    [Fact]
    public async Task GetAllSeasonsAsync_ParsesSeasonDataCorrectly()
    {
        // Arrange & Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert
        var seasonList = seasons.ToList();
        
        // Verify each season has required data parsed
        foreach (var season in seasonList)
        {
            season.Year.Should().NotBeNullOrEmpty("year should be parsed");
            season.Year.Should().MatchRegex(@"^\d{4}$", "year should be 4 digits");
            
            season.Url.Should().NotBeNullOrEmpty("URL should be parsed");
            season.Url.Should().StartWith("http", "URL should be valid");
        }
    }

    [Fact]
    public async Task GetAllSeasonsAsync_StoresSeasonsInRepository()
    {
        // Arrange
        _seasonRepository.Clear();

        // Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert
        var repositorySeasons = _seasonRepository.GetAll().ToList();
        repositorySeasons.Should().HaveCount(seasons.Count(), "all fetched seasons should be stored in repository");
    }

    [Fact]
    public async Task GetSeasonByYearAsync_SpecificYear_RetrievesCorrectSeason()
    {
        // Arrange
        var year = "2024";
        
        // Act
        var season = await _seasonService.GetSeasonByYearAsync(year);

        // Assert
        season.Should().NotBeNull();
        season!.Year.Should().Be(year);
        season.Url.Should().NotBeNullOrEmpty();
        season.Url.Should().Contain("2024");
    }

    [Fact]
    public async Task GetSeasonByYearAsync_HistoricalSeason_RetrievesCorrectSeason()
    {
        // Arrange
        var year = "1950";
        
        // Act
        var season = await _seasonService.GetSeasonByYearAsync(year);

        // Assert
        season.Should().NotBeNull();
        season!.Year.Should().Be(year);
        season.Url.Should().Contain("1950");
    }

    [Fact]
    public async Task GetSeasonByYearAsync_UsesRepositoryCache()
    {
        // Arrange
        _seasonRepository.Clear();
        var year = "2023";
        
        // First call should fetch from API
        await _seasonService.GetSeasonByYearAsync(year);
        
        // Clear the flag by checking repository
        var cachedBefore = _seasonRepository.GetByYear(year);
        cachedBefore.Should().NotBeNull("season should be cached after first call");

        // Act - Second call should use cache
        var season = await _seasonService.GetSeasonByYearAsync(year);

        // Assert
        season.Should().NotBeNull();
        season!.Year.Should().Be(year);
        season.Should().Be(cachedBefore, "should return the same cached instance");
    }

    [Fact]
    public async Task GetCachedSeasons_AfterFetchingAll_ReturnsAllSeasons()
    {
        // Arrange
        _seasonRepository.Clear();
        await _seasonService.GetAllSeasonsAsync();

        // Act
        var cachedSeasons = _seasonService.GetCachedSeasons().ToList();

        // Assert
        cachedSeasons.Should().NotBeEmpty();
        cachedSeasons.Should().HaveCountGreaterThan(70, "should have all fetched seasons in cache");
    }

    [Fact]
    public async Task GetCachedSeasons_ReturnsOrderedByYear()
    {
        // Arrange
        _seasonRepository.Clear();
        await _seasonService.GetAllSeasonsAsync();

        // Act
        var cachedSeasons = _seasonService.GetCachedSeasons().ToList();

        // Assert
        cachedSeasons.Should().NotBeEmpty();
        var years = cachedSeasons.Select(s => int.Parse(s.Year)).ToList();
        years.Should().BeInAscendingOrder("seasons should be ordered by year");
    }

    [Fact]
    public async Task GetAllSeasonsAsync_ValidatesJsonStructure_ParsesMRDataCorrectly()
    {
        // Arrange & Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert - This verifies the JSON parsing works correctly
        seasons.Should().NotBeNull();
        seasons.Should().BeAssignableTo<IEnumerable<Season>>();
        
        var firstSeason = seasons.FirstOrDefault();
        firstSeason.Should().NotBeNull();
        
        // Verify the nested JSON structure was parsed (MRData -> SeasonTable -> Seasons)
        firstSeason!.Year.Should().NotBeNullOrEmpty("the API response MRData.SeasonTable.Seasons should be parsed correctly");
    }

    [Fact]
    public async Task GetAllSeasonsAsync_IncludesRecentSeasons()
    {
        // Arrange & Act
        var seasons = await _seasonService.GetAllSeasonsAsync();

        // Assert
        var seasonList = seasons.ToList();
        var years = seasonList.Select(s => s.Year).ToList();
        
        // Verify recent years are included
        years.Should().Contain("2024", "2024 season should be included");
        years.Should().Contain("2023", "2023 season should be included");
        years.Should().Contain("2022", "2022 season should be included");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
