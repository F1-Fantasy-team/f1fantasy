using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace F1Fantasy.Tests;

public class StatusServiceIntegrationTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly StatusRepository _repository;
    private readonly StatusService _service;
    private readonly HttpClient _httpClient;
    private readonly ILogger<StatusRepository> _repositoryLogger;
    private readonly ILogger<StatusService> _serviceLogger;

    public StatusServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql("Host=dpg-d6c4j29r0fns73aujk90-a.virginia-postgres.render.com;Database=fantasyf1;Username=fantasyf1;Password=U0ZZOxG4ai4LmSA2B0FSwoSApn0PqhMs")
            .Options;

        _context = new F1FantasyDbContext(options);

        var repositoryLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _repositoryLogger = repositoryLoggerFactory.CreateLogger<StatusRepository>();

        var serviceLoggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        _serviceLogger = serviceLoggerFactory.CreateLogger<StatusService>();

        _repository = new StatusRepository(_context, _repositoryLogger);
        _httpClient = new HttpClient();
        _service = new StatusService(_httpClient, _repository, _serviceLogger);
    }

    [Fact]
    public async Task GetAllStatusesAsync_FetchesAndParsesAllStatuses()
    {
        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterOrEqualTo(100); // API returns 100 statuses (pagination limit)
        
        // Verify common statuses exist
        result.Should().Contain(s => s.StatusText == "Finished");
        result.Should().Contain(s => s.StatusText == "Engine");
        result.Should().Contain(s => s.StatusText == "Accident");
        
        // Verify structure of first status
        var firstStatus = result.First();
        firstStatus.StatusId.Should().NotBeEmpty();
        firstStatus.StatusText.Should().NotBeEmpty();
        firstStatus.Count.Should().NotBeEmpty();
        
        // Verify count is numeric
        int.Parse(firstStatus.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllStatusesAsync_StoresStatusesInDatabase()
    {
        // Act
        var result = await _service.GetAllStatusesAsync();

        // Assert
        result.Should().NotBeEmpty();
        
        // Verify data was stored in database
        var storedStatuses = await _context.Statuses.ToListAsync();

        storedStatuses.Should().NotBeEmpty();
        storedStatuses.Should().HaveCount(result.Count);
        
        // Verify "Finished" status exists in DB
        var finishedStatus = storedStatuses.FirstOrDefault(s => s.StatusText == "Finished");
        finishedStatus.Should().NotBeNull();
        finishedStatus!.StatusId.Should().Be("1"); // "Finished" is always statusId "1"
    }

    [Fact]
    public async Task GetByIdAsync_FetchesSpecificStatus()
    {
        // Arrange
        var statusId = "1"; // "Finished" status

        // Act
        var result = await _service.GetByIdAsync(statusId);

        // Assert
        result.Should().NotBeNull();
        result!.StatusId.Should().Be(statusId);
        result.StatusText.Should().Be("Finished");
        
        // Count should be a valid number
        int.Parse(result.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByTextAsync_FetchesStatusByText()
    {
        // Arrange
        var statusText = "Engine";

        // Act
        var result = await _service.GetByTextAsync(statusText);

        // Assert
        result.Should().NotBeNull();
        result!.StatusText.Should().Be(statusText);
        result.StatusId.Should().NotBeEmpty();
        
        // Count should be a valid number
        int.Parse(result.Count).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RefreshStatusesAsync_UpdatesCacheWithLatestData()
    {
        // Arrange - First populate cache
        await _service.GetAllStatusesAsync();

        // Act - Refresh
        var result = await _service.RefreshStatusesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterOrEqualTo(100); // API has pagination limit of 100
        
        // Verify data is sorted by count (descending)
        var counts = result.Take(10).Select(s => int.Parse(s.Count)).ToList();
        counts.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAllStatusesAsync_ReturnsFromCacheOnSecondCall()
    {
        // Arrange - First call populates cache
        var firstResult = await _service.GetAllStatusesAsync();

        // Act - Second call should use cache
        var secondResult = await _service.GetAllStatusesAsync();

        // Assert
        secondResult.Should().NotBeEmpty();
        secondResult.Should().HaveCount(firstResult.Count);
        secondResult.Should().BeEquivalentTo(firstResult);
    }

    public void Dispose()
    {
        // Clean up test data - remove all statuses from cache
        var allStatuses = _context.Statuses.ToList();
        if (allStatuses.Any())
        {
            _context.Statuses.RemoveRange(allStatuses);
            _context.SaveChanges();
        }

        _context.Dispose();
        _httpClient.Dispose();
    }
}
