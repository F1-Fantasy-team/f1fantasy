using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for RaceService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can actually fetch and parse data from the real API
/// and store data in PostgreSQL database
/// </summary>
public class RaceServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RaceRepository _raceRepository;
    private readonly RaceService _raceService;

    public RaceServiceIntegrationTests()
    {
        _httpClient = new HttpClient();
        
        // Load environment variables from .env file
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        
        // Get connection string directly from environment variable (loaded by DotNetEnv)
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Database connection string not found. Ensure .env file exists at {envPath} and contains ConnectionStrings__DefaultConnection");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        var context = new F1FantasyDbContext(options);
        _raceRepository = new RaceRepository(context);
        _raceService = new RaceService(_httpClient, _raceRepository);
    }

    [Fact]
    public async Task GetRacesForSeasonAsync_2024Season_FetchesAndParsesRacesCorrectly()
    {
        // Arrange
        var season = "2024";

        // Act
        var races = await _raceService.GetRacesForSeasonAsync(season);

        // Assert
        races.Should().NotBeNull();
        races.Should().NotBeEmpty("the 2024 season had multiple races");
        
        var raceList = races.ToList();
        raceList.Should().HaveCountGreaterThan(10, "F1 2024 season had 24 races");

        // Verify first race structure is parsed correctly
        var firstRace = raceList.First();
        firstRace.Season.Should().Be("2024");
        firstRace.Round.Should().NotBeNullOrEmpty();
        firstRace.RaceName.Should().NotBeNullOrEmpty();
        firstRace.Circuit.Should().NotBeNull();
        firstRace.Circuit.CircuitName.Should().NotBeNullOrEmpty();
        firstRace.Circuit.Location.Should().NotBeNull();
        firstRace.Circuit.Location.Country.Should().NotBeNullOrEmpty();
        firstRace.Circuit.Location.Locality.Should().NotBeNullOrEmpty();
        firstRace.Date.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRacesForSeasonAsync_2023Season_ParsesAllRaceDetails()
    {
        // Arrange
        var season = "2023";

        // Act
        var races = await _raceService.GetRacesForSeasonAsync(season);

        // Assert
        var raceList = races.ToList();
        raceList.Should().NotBeEmpty();

        // Verify each race has required data parsed
        foreach (var race in raceList)
        {
            race.Season.Should().Be("2023");
            race.Round.Should().NotBeNullOrEmpty();
            race.RaceName.Should().NotBeNullOrEmpty();
            race.Url.Should().StartWith("http");
            race.Date.Should().NotBeNullOrEmpty();
            
            // Circuit details
            race.Circuit.CircuitId.Should().NotBeNullOrEmpty();
            race.Circuit.CircuitName.Should().NotBeNullOrEmpty();
            race.Circuit.Url.Should().StartWith("http");
            
            // Location details
            race.Circuit.Location.Locality.Should().NotBeNullOrEmpty();
            race.Circuit.Location.Country.Should().NotBeNullOrEmpty();
            race.Circuit.Location.Lat.Should().NotBeNullOrEmpty();
            race.Circuit.Location.Long.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetRacesForSeasonAsync_StoresRacesInRepository()
    {
        // Arrange
        var season = "2024";
        await _raceRepository.ClearAsync();

        // Act
        var races = await _raceService.GetRacesForSeasonAsync(season);

        // Assert
        var repositoryRaces = (await _raceRepository.GetBySeasonAsync(season)).ToList();
        repositoryRaces.Should().HaveCount(races.Count(), "all fetched races should be stored in repository");
    }

    [Fact]
    public async Task GetRacesForSeasonAsync_SpecificRace_ParsesSessionInformation()
    {
        // Arrange
        var season = "2024";

        // Act
        var races = await _raceService.GetRacesForSeasonAsync(season);

        // Assert
        var raceList = races.ToList();
        raceList.Should().NotBeEmpty();

        // At least some races should have session information
        var racesWithQualifying = raceList.Where(r => r.Qualifying != null).ToList();
        racesWithQualifying.Should().NotBeEmpty("races should have qualifying session data");

        foreach (var race in racesWithQualifying)
        {
            race.Qualifying!.Date.Should().NotBeNullOrEmpty();
            race.Qualifying.Time.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetRaceByRoundAsync_AfterFetchingSeason_RetrievesCorrectRace()
    {
        // Arrange
        var season = "2024";
        var round = "1";
        
        // Fetch all races first
        await _raceService.GetRacesForSeasonAsync(season);

        // Act
        var race = await _raceService.GetRaceByRoundAsync(season, round);

        // Assert
        race.Should().NotBeNull();
        race!.Season.Should().Be(season);
        race.Round.Should().Be(round);
        race.RaceName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAllRacesAsync_AfterFetchingMultipleSeasons_ReturnsAllStoredRaces()
    {
        // Arrange
        await _raceRepository.ClearAsync();
        await _raceService.GetRacesForSeasonAsync("2023");
        await _raceService.GetRacesForSeasonAsync("2024");

        // Act
        var allRaces = await _raceService.GetAllRacesAsync();

        // Assert
        var raceList = allRaces.ToList();
        raceList.Should().NotBeEmpty();
        raceList.Should().Contain(r => r.Season == "2023");
        raceList.Should().Contain(r => r.Season == "2024");
    }

    [Fact]
    public async Task GetRacesForSeasonAsync_ValidatesJsonStructure_ParsesMRDataCorrectly()
    {
        // Arrange
        var season = "2024";

        // Act
        var races = await _raceService.GetRacesForSeasonAsync(season);

        // Assert - This verifies the JSON parsing works correctly
        races.Should().NotBeNull();
        races.Should().BeAssignableTo<IEnumerable<Race>>();
        
        var firstRace = races.FirstOrDefault();
        firstRace.Should().NotBeNull();
        
        // Verify the nested JSON structure was parsed (MRData -> RaceTable -> Races)
        firstRace!.RaceName.Should().NotBeNullOrEmpty("the API response MRData.RaceTable.Races should be parsed correctly");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
