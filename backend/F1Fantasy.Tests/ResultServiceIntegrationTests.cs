using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for ResultService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can fetch and parse race results from the real API
/// and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class ResultServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ResultRepository _resultRepository;
    private readonly ResultService _resultService;
    private readonly F1FantasyDbContext _context;

    public ResultServiceIntegrationTests()
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
        _resultRepository = new ResultRepository(_context, NullLogger<ResultRepository>.Instance);
        _resultService = new ResultService(_httpClient, _resultRepository, NullLogger<ResultService>.Instance);
    }

    [Fact]
    public async Task GetResultsBySeasonAsync_FetchesAndParsesResults()
    {
        // Arrange
        var season = "2025";

        // Act
        var races = await _resultService.GetResultsBySeasonAsync(season);

        // Assert
        races.Should().NotBeNull();
        races.Should().NotBeEmpty("2025 season should have races");
        
        var raceList = races.ToList();
        raceList.Should().HaveCountGreaterThan(0, "2025 season has races");

        // Verify first race structure
        var firstRace = raceList.First();
        firstRace.Season.Should().Be(season);
        firstRace.Round.Should().NotBeNullOrEmpty();
        firstRace.RaceName.Should().NotBeNullOrEmpty();
        firstRace.Results.Should().NotBeNull();
        firstRace.Results.Should().NotBeEmpty("race should have results");

        // Verify result structure
        var firstResult = firstRace.Results!.First();
        firstResult.Position.Should().Be("1", "first result should be P1");
        firstResult.Driver.Should().NotBeNull();
        firstResult.Driver.DriverId.Should().NotBeNullOrEmpty();
        firstResult.Driver.GivenName.Should().NotBeNullOrEmpty();
        firstResult.Driver.FamilyName.Should().NotBeNullOrEmpty();
        firstResult.Constructor.Should().NotBeNull();
        firstResult.Constructor.ConstructorId.Should().NotBeNullOrEmpty();
        firstResult.Constructor.Name.Should().NotBeNullOrEmpty();
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetResultsByRaceAsync_FetchesSpecificRaceResults()
    {
        // Arrange
        var season = "2025";
        var round = "1";

        // Act
        var race = await _resultService.GetResultsByRaceAsync(season, round);

        // Assert
        race.Should().NotBeNull();
        race!.Season.Should().Be(season);
        race.Round.Should().Be(round);
        race.Results.Should().NotBeNull();
        race.Results.Should().NotBeEmpty("Australian GP 2025 should have results");
        race.Results!.Count.Should().BeGreaterThan(15, "F1 race should have multiple finishers");

        // Verify winner data
        var winner = race.Results.First();
        winner.Position.Should().Be("1");
        winner.Points.Should().Be("25", "winner gets 25 points");
        winner.Status.Should().Be("Finished", "winner should have finished");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetResultsByRaceAsync_StoresResultsInDatabase()
    {
        // Arrange
        var season = "2025";
        var round = "1";

        // Act
        await _resultService.GetResultsByRaceAsync(season, round);
        var cachedResults = await _resultRepository.GetByRaceAsync(season, round);

        // Assert
        cachedResults.Should().NotBeEmpty("results should be cached in database");
        cachedResults.Should().HaveCountGreaterThan(15, "should have multiple results stored");
        
        var cachedWinner = cachedResults.First();
        cachedWinner.Position.Should().Be("1");
        cachedWinner.Season.Should().Be(season);
        cachedWinner.Round.Should().Be(round);
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetResultByDriverAsync_FetchesSpecificDriverResult()
    {
        // Arrange
        var season = "2025";
        var round = "1";
        var driverId = "norris"; // Lando Norris won Australian GP 2025

        // Act - First fetch the race to populate cache
        var race = await _resultService.GetResultsByRaceAsync(season, round);
        
        // Then get specific driver result
        var result = await _resultService.GetResultByDriverAsync(season, round, driverId);

        // Assert
        result.Should().NotBeNull();
        result!.Season.Should().Be(season);
        result.Round.Should().Be(round);
        result.DriverId.Should().Be(driverId, "DriverId field should be populated from API data");
        result.Position.Should().Be("1", "Norris won the 2025 Australian GP");
        result.Points.Should().Be("25");
        
        await Task.Delay(1000); // Polite delay after test
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _context?.Dispose();
    }
}
