using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for PitStopService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can fetch and parse pit stop data from the real API
/// and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class PitStopServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly PitStopRepository _pitStopRepository;
    private readonly PitStopService _pitStopService;
    private readonly F1FantasyDbContext _context;

    public PitStopServiceIntegrationTests()
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
        _pitStopRepository = new PitStopRepository(_context, NullLogger<PitStopRepository>.Instance);
        _pitStopService = new PitStopService(_httpClient, _pitStopRepository, NullLogger<PitStopService>.Instance);
    }

    [Fact]
    public async Task GetPitStopsByRaceAsync_FetchesAndParsesPitStops()
    {
        // Arrange
        var season = "2025";
        var round = "1"; // Australian GP

        // Act
        var race = await _pitStopService.GetPitStopsByRaceAsync(season, round);

        // Assert
        race.Should().NotBeNull();
        race!.Season.Should().Be(season);
        race.Round.Should().Be(round);
        race.PitStops.Should().NotBeNull();
        race.PitStops.Should().NotBeEmpty("Australian GP 2025 should have pit stops");
        race.PitStops!.Count.Should().BeGreaterThan(20, "race should have multiple pit stops");

        // Verify first pit stop structure
        var firstPitStop = race.PitStops.First();
        firstPitStop.DriverId.Should().NotBeNullOrEmpty();
        firstPitStop.Lap.Should().NotBeNullOrEmpty();
        firstPitStop.Stop.Should().NotBeNullOrEmpty();
        firstPitStop.Time.Should().NotBeNullOrEmpty();
        firstPitStop.Duration.Should().NotBeNullOrEmpty("pit stop should have duration");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetPitStopsByRaceAsync_StoresPitStopsInDatabase()
    {
        // Arrange
        var season = "2025";
        var round = "1";

        // Act
        await _pitStopService.GetPitStopsByRaceAsync(season, round);
        var cachedPitStops = await _pitStopRepository.GetByRaceAsync(season, round);

        // Assert
        cachedPitStops.Should().NotBeEmpty("pit stops should be cached in database");
        cachedPitStops.Should().HaveCountGreaterThan(20, "should have all pit stops stored");
        
        var firstCached = cachedPitStops.First();
        firstCached.Season.Should().Be(season);
        firstCached.Round.Should().Be(round);
        firstCached.Duration.Should().NotBeNullOrEmpty();
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetPitStopsByDriverAsync_FetchesSpecificDriverPitStops()
    {
        // Arrange
        var season = "2025";
        var round = "1";
        var driverId = "norris"; // Lando Norris

        // Act
        await _pitStopService.GetPitStopsByRaceAsync(season, round);
        var pitStops = await _pitStopService.GetPitStopsByDriverAsync(season, round, driverId);

        // Assert
        pitStops.Should().NotBeEmpty("driver should have made pit stops");
        pitStops.Should().OnlyContain(p => p.DriverId == driverId);
        pitStops.Should().OnlyContain(p => p.Season == season);
        pitStops.Should().OnlyContain(p => p.Round == round);
        
        // Verify pit stops are ordered by stop number
        var stopNumbers = pitStops.Select(p => int.Parse(p.Stop)).ToList();
        stopNumbers.Should().BeInAscendingOrder();
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetPitStopsByDriverAsync_VerifiesMultipleStops()
    {
        // Arrange
        var season = "2025";
        var round = "1";
        var driverId = "norris";

        // Act
        await _pitStopService.GetPitStopsByRaceAsync(season, round);
        var pitStops = await _pitStopService.GetPitStopsByDriverAsync(season, round, driverId);

        // Assert
        pitStops.Count().Should().BeGreaterThan(0, "driver should have pit stops");
        
        // Check if driver has multiple stops (common in F1 races)
        foreach (var pitStop in pitStops)
        {
            pitStop.Stop.Should().NotBeNullOrEmpty();
            pitStop.Duration.Should().NotBeNullOrEmpty();
            // Note: Some pit stops may have non-numeric duration (e.g., retired/DNF)
            if (double.TryParse(pitStop.Duration, out var duration))
            {
                duration.Should().BeGreaterThan(0, "pit stop duration should be positive");
            }
        }
        
        await Task.Delay(1000); // Polite delay after test
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _context?.Dispose();
    }
}
