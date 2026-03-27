using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for DriverStandingService that make real API calls to https://api.jolpi.ca/ergast/f1
/// These tests verify that the service can fetch and parse driver standings data from the real API
/// and store data in PostgreSQL database
/// </summary>
[Collection("Sequential Integration Tests")]
public class DriverStandingServiceIntegrationTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DriverStandingRepository _repository;
    private readonly DriverStandingService _service;
    private readonly F1FantasyDbContext _context;

    public DriverStandingServiceIntegrationTests()
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
        _repository = new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance);
        var metadataRepository = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepository = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepository, raceRepository, NullLogger<CacheStalenessService>.Instance);
        _service = new DriverStandingService(_httpClient, _repository, metadataRepository, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
    }

    [Fact]
    public async Task GetDriverStandingsBySeasonAsync_FetchesAndParsesStandings()
    {
        // Arrange
        var season = "2025";

        // Act
        var standings = await _service.GetDriverStandingsBySeasonAsync(season);

        // Assert
        standings.Should().NotBeNull();
        standings!.Season.Should().Be(season);
        standings.Round.Should().NotBeNullOrEmpty();
        standings.DriverStandings.Should().NotBeNull();
        standings.DriverStandings.Should().NotBeEmpty("2025 season should have driver standings");
        standings.DriverStandings!.Count.Should().BeGreaterThan(15, "F1 has around 20 drivers");

        // Verify first standing structure
        var firstStanding = standings.DriverStandings.First();
        firstStanding.Position.Should().NotBeNullOrEmpty();
        firstStanding.PositionText.Should().NotBeNullOrEmpty();
        firstStanding.Points.Should().NotBeNullOrEmpty();
        firstStanding.Wins.Should().NotBeNullOrEmpty();
        firstStanding.Driver.Should().NotBeNull();
        firstStanding.Driver!.DriverId.Should().NotBeNullOrEmpty();
        firstStanding.Constructors.Should().NotBeNull();
        firstStanding.Constructors.Should().NotBeEmpty();
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriverStandingsBySeasonAsync_StoresStandingsInDatabase()
    {
        // Arrange
        var season = "2025";

        // Act
        await _service.GetDriverStandingsBySeasonAsync(season);
        var cachedStandings = await _repository.GetBySeasonAsync(season);

        // Assert
        cachedStandings.Should().NotBeEmpty("standings should be cached in database");
        cachedStandings.Should().HaveCountGreaterThan(15, "should have all driver standings stored");
        
        var firstCached = cachedStandings.First();
        firstCached.Season.Should().Be(season);
        firstCached.Position.Should().NotBeNullOrEmpty();
        firstCached.Points.Should().NotBeNullOrEmpty();
        firstCached.DriverId.Should().NotBeNullOrEmpty();
        firstCached.ConstructorId.Should().NotBeNullOrEmpty();
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriverStandingsByRoundAsync_FetchesSpecificRoundStandings()
    {
        // Arrange
        var season = "2025";
        var round = "10";

        // Act
        var standings = await _service.GetDriverStandingsByRoundAsync(season, round);

        // Assert
        standings.Should().NotBeNull();
        standings!.Season.Should().Be(season);
        standings.Round.Should().Be(round);
        standings.DriverStandings.Should().NotBeEmpty();
        
        // Verify standings are ordered by position
        var positions = standings.DriverStandings!.Select(s => int.Parse(s.Position)).ToList();
        positions.Should().BeInAscendingOrder();
        positions.First().Should().Be(1, "first position should be 1");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetDriverStandingByDriverAsync_FetchesSpecificDriverStanding()
    {
        // Arrange
        var season = "2025";
        var round = "10";
        var driverId = "norris"; // Lando Norris

        // Act
        await _service.GetDriverStandingsByRoundAsync(season, round);
        var standing = await _service.GetDriverStandingByDriverAsync(season, round, driverId);

        // Assert
        standing.Should().NotBeNull();
        standing!.Season.Should().Be(season);
        standing.DriverId.Should().Be(driverId);
        standing.Position.Should().NotBeNullOrEmpty();
        standing.Points.Should().NotBeNullOrEmpty();
        int.Parse(standing.Points).Should().BeGreaterThan(0, "driver should have points");
        
        await Task.Delay(1000); // Polite delay after test
    }

    [Fact]
    public async Task GetCachedStandingsAsync_ReturnsAllCachedStandings()
    {
        // Arrange
        var season = "2025";
        
        // Act - First fetch and store some data
        await _service.GetDriverStandingsBySeasonAsync(season);
        var cachedStandings = await _service.GetCachedStandingsAsync();

        // Assert
        cachedStandings.Should().NotBeEmpty();
        cachedStandings.Should().AllSatisfy(s =>
        {
            s.Season.Should().NotBeNullOrEmpty();
            s.DriverId.Should().NotBeNullOrEmpty();
            s.Position.Should().NotBeNullOrEmpty();
            s.Points.Should().NotBeNullOrEmpty();
        });
        
        await Task.Delay(1000); // Polite delay after test
    }

    public void Dispose()
    {
        // Clean up test data only - remove only the test standings we inserted (2025 season)
        var testData = _context.DriverStandings
            .Where(ds => ds.Season == "2025")
            .ToList();
        
        if (testData.Any())
        {
            _context.DriverStandings.RemoveRange(testData);
            _context.SaveChanges();
        }
        
        _httpClient?.Dispose();
        _context?.Dispose();
    }
}
