using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for QualifyingService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can fetch and parse qualifying data from the real API
/// and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class QualifyingServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly QualifyingService _qualifyingService;
    private readonly F1FantasyDbContext _context;

    public QualifyingServiceIntegrationTests()
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
        _context = new F1FantasyDbContext(options);
        _qualifyingRepository = new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance);
        _qualifyingService = new QualifyingService(_httpClient, _qualifyingRepository, new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance), new RaceRepository(_context, NullLogger<RaceRepository>.Instance), NullLogger<QualifyingService>.Instance);
    }

    [Fact]
    public async Task GetQualifyingBySeasonAsync_FetchesAndParsesQualifying()
    {
        // Arrange
        var season = "2025";

        // Act
        var races = await _qualifyingService.GetQualifyingBySeasonAsync(season);

        // Assert
        races.Should().NotBeNull();
        races.Should().NotBeEmpty("2025 season should have qualifying sessions");
        
        var raceList = races.ToList();
        raceList.Should().HaveCountGreaterThan(0, "2025 season has races with qualifying");

        // Verify first race structure
        var firstRace = raceList.First();
        firstRace.Season.Should().Be(season);
        firstRace.Round.Should().NotBeNullOrEmpty();
        firstRace.RaceName.Should().NotBeNullOrEmpty();
        firstRace.QualifyingResults.Should().NotBeNull();
        firstRace.QualifyingResults.Should().NotBeEmpty("race should have qualifying results");

        // Verify qualifying structure
        var polePosition = firstRace.QualifyingResults!.First();
        polePosition.Position.Should().Be("1", "first result should be P1 (pole position)");
        polePosition.Driver.Should().NotBeNull();
        polePosition.Driver.DriverId.Should().NotBeNullOrEmpty();
        polePosition.Constructor.Should().NotBeNull();
        polePosition.Constructor.ConstructorId.Should().NotBeNullOrEmpty();
        polePosition.Q1.Should().NotBeNullOrEmpty("pole sitter should have Q1 time");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetQualifyingByRaceAsync_FetchesSpecificQualifying()
    {
        // Arrange
        var season = "2025";
        var round = "1"; // Australian GP

        // Act
        var race = await _qualifyingService.GetQualifyingByRaceAsync(season, round);

        // Assert
        race.Should().NotBeNull();
        race!.Season.Should().Be(season);
        race.Round.Should().Be(round);
        race.QualifyingResults.Should().NotBeNull();
        race.QualifyingResults.Should().NotBeEmpty("Australian GP 2025 should have qualifying results");
        race.QualifyingResults!.Count.Should().BeGreaterOrEqualTo(20, "F1 qualifying should have 20 drivers");

        // Verify pole position data (Lando Norris got pole in 2025 Australian GP)
        var polePosition = race.QualifyingResults.First();
        polePosition.Position.Should().Be("1");
        polePosition.Driver.DriverId.Should().Be("norris");
        polePosition.Q1.Should().NotBeNullOrEmpty("pole sitter should have Q1 time");
        polePosition.Q2.Should().NotBeNullOrEmpty("pole sitter should have Q2 time");
        polePosition.Q3.Should().NotBeNullOrEmpty("pole sitter should have Q3 time");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetQualifyingByRaceAsync_StoresQualifyingInDatabase()
    {
        // Arrange
        var season = "2025";
        var round = "1";

        // Act
        await _qualifyingService.GetQualifyingByRaceAsync(season, round);
        var cachedQualifying = await _qualifyingRepository.GetByRaceAsync(season, round);

        // Assert
        cachedQualifying.Should().NotBeEmpty("qualifying should be cached in database");
        cachedQualifying.Should().HaveCountGreaterOrEqualTo(20, "should have all drivers stored");
        
        var cachedPole = cachedQualifying.First();
        cachedPole.Position.Should().Be("1");
        cachedPole.Season.Should().Be(season);
        cachedPole.Round.Should().Be(round);
        cachedPole.DriverId.Should().Be("norris");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetQualifyingByDriverAsync_FetchesSpecificDriverQualifying()
    {
        // Arrange
        var season = "2025";
        var round = "1";
        var driverId = "norris"; // Lando Norris got pole in Australian GP 2025

        // Act - First fetch the race to populate cache
        await _qualifyingService.GetQualifyingByRaceAsync(season, round);
        
        // Then get specific driver qualifying
        var qualifying = await _qualifyingService.GetQualifyingByDriverAsync(season, round, driverId);

        // Assert
        qualifying.Should().NotBeNull();
        qualifying!.Season.Should().Be(season);
        qualifying.Round.Should().Be(round);
        qualifying.DriverId.Should().Be(driverId);
        qualifying.Position.Should().Be("1", "Norris got pole in 2025 Australian GP");
        qualifying.Q3.Should().NotBeNullOrEmpty("Norris made it to Q3");
        
        await Task.Delay(1000); // Polite delay after test
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _context?.Dispose();
    }
}
