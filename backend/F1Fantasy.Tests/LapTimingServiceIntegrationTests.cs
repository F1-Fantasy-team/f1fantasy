using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for LapTimingService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can fetch and parse lap timing data from the real API
/// and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class LapTimingServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly LapTimingRepository _repository;
    private readonly LapTimingService _service;
    private readonly F1FantasyDbContext _context;

    public LapTimingServiceIntegrationTests()
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
        
        // Use console logger to see what's happening
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var repositoryLogger = loggerFactory.CreateLogger<LapTimingRepository>();
        var serviceLogger = loggerFactory.CreateLogger<LapTimingService>();
        
        _repository = new LapTimingRepository(_context, repositoryLogger);
        _service = new LapTimingService(_httpClient, _repository, serviceLogger);
    }

    [Fact]
    public async Task GetLapsByRaceAsync_ShouldFetchAndStoreData()
    {
        // Arrange
        var season = "2024";
        var round = "1";

        // Act
        var result = await _service.GetLapsByRaceAsync(season, round);

        // Debug: If null, let's see why
        if (result == null)
        {
            var allTimings = await _repository.GetAllAsync();
            Console.WriteLine($"DEBUG: Result was null. Total lap timings in DB: {allTimings.Count()}");
            
            // Try direct API call to see if it works
            var testHttp = new HttpClient();
            var testResponse = await testHttp.GetAsync("https://api.jolpi.ca/ergast/f1/2024/1/laps.json?limit=1");
            var testContent = await testResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG: Direct API call status: {testResponse.StatusCode}");
            Console.WriteLine($"DEBUG: Content length: {testContent.Length}");
        }

        // Assert
        result.Should().NotBeNull();
        result!.Season.Should().Be(season);
        result.Round.Should().Be(round);
        result.Laps.Should().NotBeEmpty();
        result.Laps.Should().HaveCountGreaterThan(0);

        // Verify data is stored in database
        var storedTimings = await _repository.GetByRaceAsync(season, round);
        storedTimings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLapsByRaceAsync_ShouldStoreFlattenedLapTimings()
    {
        // Arrange
        var season = "2024";
        var round = "1";

        // Act
        await _service.GetLapsByRaceAsync(season, round);

        // Assert - verify lap timings are properly flattened
        var storedTimings = await _repository.GetByRaceAsync(season, round);
        storedTimings.Should().NotBeEmpty();

        var firstLapTimings = storedTimings.Where(t => t.LapNumber == "1").ToList();
        firstLapTimings.Should().NotBeEmpty();
        firstLapTimings.Should().AllSatisfy(t =>
        {
            t.Season.Should().Be(season);
            t.Round.Should().Be(round);
            t.LapNumber.Should().Be("1");
            t.DriverId.Should().NotBeNullOrEmpty();
            t.Position.Should().NotBeNullOrEmpty();
            t.Time.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetLapByNumberAsync_ShouldReturnSpecificLap()
    {
        // Arrange
        var season = "2024";
        var round = "1";
        var lapNumber = "1";

        // Act - First fetch all laps to populate cache
        await _service.GetLapsByRaceAsync(season, round);
        var result = await _service.GetLapByNumberAsync(season, round, lapNumber);

        // Assert
        result.Should().NotBeNull();
        result!.Number.Should().Be(lapNumber);
        result.Timings.Should().NotBeEmpty();
        result.Timings.Should().AllSatisfy(t =>
        {
            t.LapNumber.Should().Be(lapNumber);
        });
    }

    [Fact]
    public async Task GetLapsByDriverAsync_ShouldReturnDriverLaps()
    {
        // Arrange
        var season = "2024";
        var round = "1";

        // Act - First fetch all laps to populate cache
        await _service.GetLapsByRaceAsync(season, round);
        
        // Get a driver ID from the stored data
        var allTimings = await _repository.GetByRaceAsync(season, round);
        var driverId = allTimings.First().DriverId;

        var result = await _service.GetLapsByDriverAsync(season, round, driverId);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(t =>
        {
            t.DriverId.Should().Be(driverId);
            t.Season.Should().Be(season);
            t.Round.Should().Be(round);
        });
    }

    [Fact]
    public async Task GetCachedLapsAsync_ShouldReturnStoredTimings()
    {
        // Arrange
        var season = "2024";
        var round = "1";

        // Act - First fetch and store some data
        await _service.GetLapsByRaceAsync(season, round);
        var cachedLaps = await _service.GetCachedLapsAsync();

        // Assert
        cachedLaps.Should().NotBeEmpty();
        cachedLaps.Should().AllSatisfy(t =>
        {
            t.Season.Should().NotBeNullOrEmpty();
            t.Round.Should().NotBeNullOrEmpty();
            t.LapNumber.Should().NotBeNullOrEmpty();
            t.DriverId.Should().NotBeNullOrEmpty();
        });
    }

    public void Dispose()
    {
        // Clean up test data only - DO NOT delete the entire database!
        // Remove only the test lap timings we inserted (2024 season round 1)
        var testData = _context.LapTimings
            .Where(lt => lt.Season == "2024" && lt.Round == "1")
            .ToList();
        
        if (testData.Any())
        {
            _context.LapTimings.RemoveRange(testData);
            _context.SaveChanges();
        }
        
        _context.Dispose();
    }
}
