using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace F1Fantasy.Tests;

public class ConstructorStandingServiceIntegrationTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly ConstructorStandingRepository _repository;
    private readonly ConstructorStandingService _service;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ConstructorStandingRepository> _repositoryLogger;
    private readonly ILogger<ConstructorStandingService> _serviceLogger;

    public ConstructorStandingServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql("Host=dpg-d6c4j29r0fns73aujk90-a.virginia-postgres.render.com;Database=fantasyf1;Username=fantasyf1;Password=U0ZZOxG4ai4LmSA2B0FSwoSApn0PqhMs")
            .Options;

        _context = new F1FantasyDbContext(options);

        var repositoryLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _repositoryLogger = repositoryLoggerFactory.CreateLogger<ConstructorStandingRepository>();

        var serviceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _serviceLogger = serviceLoggerFactory.CreateLogger<ConstructorStandingService>();

        _repository = new ConstructorStandingRepository(_context, _repositoryLogger);
        _httpClient = new HttpClient();
        var metadataRepository = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepository = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepository, raceRepository, NullLogger<CacheStalenessService>.Instance);
        _service = new ConstructorStandingService(_httpClient, _repository, metadataRepository, cacheStalenessService, _serviceLogger);
    }

    [Fact]
    public async Task GetConstructorStandingsBySeasonAsync_FetchesAndParsesStandings()
    {
        // Arrange
        var season = "2025";

        // Act
        var result = await _service.GetConstructorStandingsBySeasonAsync(season);

        // Assert
        result.Should().NotBeNull();
        result!.Season.Should().Be(season);
        result.ConstructorStandings.Should().NotBeEmpty();
        result.ConstructorStandings.Should().HaveCountGreaterThan(5); // Should have multiple teams
        
        var firstStanding = result.ConstructorStandings.First();
        firstStanding.Position.Should().NotBeEmpty();
        firstStanding.Points.Should().NotBeEmpty();
        firstStanding.Wins.Should().NotBeEmpty();
        firstStanding.Constructor.Should().NotBeNull();
        firstStanding.Constructor.ConstructorId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetConstructorStandingsBySeasonAsync_StoresStandingsInDatabase()
    {
        // Arrange
        var season = "2025";

        // Act
        var result = await _service.GetConstructorStandingsBySeasonAsync(season);

        // Assert
        result.Should().NotBeNull();
        
        // Verify data was stored in database
        var storedStandings = await _context.ConstructorStandings
            .Where(cs => cs.Season == season)
            .ToListAsync();

        storedStandings.Should().NotBeEmpty();
        storedStandings.Should().HaveCount(result!.ConstructorStandings.Count);
        
        // Verify first standing matches
        var firstApiStanding = result.ConstructorStandings.First();
        var firstDbStanding = storedStandings.First(cs => cs.ConstructorId == firstApiStanding.Constructor.ConstructorId);
        
        firstDbStanding.Season.Should().Be(season);
        firstDbStanding.Round.Should().Be(result.Round);
        firstDbStanding.Position.Should().Be(firstApiStanding.Position);
        firstDbStanding.Points.Should().Be(firstApiStanding.Points);
        firstDbStanding.Wins.Should().Be(firstApiStanding.Wins);
    }

    [Fact]
    public async Task GetConstructorStandingsByRoundAsync_FetchesSpecificRoundStandings()
    {
        // Arrange
        var season = "2025";
        var round = "10";

        // Act
        var result = await _service.GetConstructorStandingsByRoundAsync(season, round);

        // Assert
        result.Should().NotBeNull();
        result!.Season.Should().Be(season);
        result.Round.Should().Be(round);
        result.ConstructorStandings.Should().NotBeEmpty();
        
        // Verify standings are ordered by position
        var positions = result.ConstructorStandings.Select(cs => int.Parse(cs.Position)).ToList();
        positions.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetConstructorStandingByConstructorAsync_FetchesSpecificConstructorStanding()
    {
        // Arrange
        var season = "2025";
        var round = "10";
        var constructorId = "mclaren";

        // Act
        var result = await _service.GetConstructorStandingByConstructorAsync(season, round, constructorId);

        // Assert
        result.Should().NotBeNull();
        result!.Constructor.Should().NotBeNull();
        result.Constructor.ConstructorId.Should().Be(constructorId);
        result.Position.Should().NotBeEmpty();
        result.Points.Should().NotBeEmpty();
        
        // Points should be a valid number
        int.Parse(result.Points).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCachedStandingsAsync_ReturnsAllCachedStandings()
    {
        // Arrange - First populate cache by fetching standings
        await _service.GetConstructorStandingsBySeasonAsync("2025");

        // Act
        var result = await _service.GetCachedStandingsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(sl => sl.Season == "2025");
        
        var season2025Standings = result.First(sl => sl.Season == "2025");
        season2025Standings.ConstructorStandings.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        // Clean up only the test data (2025 season)
        var testStandings = _context.ConstructorStandings
            .Where(cs => cs.Season == "2025")
            .ToList();

        if (testStandings.Any())
        {
            _context.ConstructorStandings.RemoveRange(testStandings);
            _context.SaveChanges();
        }

        _context.Dispose();
        _httpClient.Dispose();
    }
}
