using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for PredictionRepository focusing on performance optimizations
/// </summary>
[Collection("Sequential")]
public class PredictionRepositoryTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly PredictionRepository _repository;
    private readonly int _testGroupId = 99999;
    private readonly string _testUserId = "test_user_predictions";

    public PredictionRepositoryTests()
    {
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Database connection string not found. Ensure .env file exists at {envPath}");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        _context = new F1FantasyDbContext(options);
        
        // Create DbContextFactory for PredictionRepository
        var contextFactory = new TestDbContextFactory(options);
        _repository = new PredictionRepository(contextFactory);
        
        // Ensure test group exists
        CreateTestGroupAsync().Wait();
    }

    private async Task CreateTestGroupAsync()
    {
        var existingGroup = await _context.Groups.FindAsync(_testGroupId);
        if (existingGroup == null)
        {
            _context.Groups.Add(new Group
            {
                Id = _testGroupId,
                Name = "Test Group for Predictions",
                AdminUserId = _testUserId,
                InviteCode = "TEST9999",
                LockMode = "manual",
                CreatedAt = DateTime.UtcNow,
                PredictionsLocked = false
            });
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAllPredictionsAsync_WithMultiplePredictions_RetrievesAllCorrectly()
    {
        // Arrange - Create test predictions
        await CleanupTestDataAsync();
        
        var constructorPrediction = new ConstructorChampionshipPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            RankedConstructorIds = new List<string> { "mercedes", "ferrari", "red_bull" }
        };
        await _repository.UpsertConstructorChampionshipAsync(constructorPrediction);

        var driverPrediction = new DriverChampionshipPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            RankedDriverIds = new List<string> { "hamilton", "verstappen", "leclerc" }
        };
        await _repository.UpsertDriverChampionshipAsync(driverPrediction);

        var driverDraft = new DriverDraftPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            Driver1Id = "hamilton",
            Driver2Id = "verstappen"
        };
        await _repository.UpsertDriverDraftAsync(driverDraft);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var predictions = await _repository.GetAllPredictionsAsync(_testGroupId, _testUserId);
        stopwatch.Stop();

        // Assert
        predictions.Should().NotBeNull();
        predictions.ConstructorChampionship.Should().NotBeNull();
        predictions.ConstructorChampionship!.RankedConstructorIds.Should().HaveCount(3);
        predictions.DriverChampionship.Should().NotBeNull();
        predictions.DriverChampionship!.RankedDriverIds.Should().HaveCount(3);
        predictions.DriverDraft.Should().NotBeNull();
        predictions.DriverDraft!.Driver1Id.Should().Be("hamilton");
        
        // Performance assertion - should complete reasonably quickly  
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "queries should execute quickly");
    }

    [Fact]
    public async Task Upsert_NewPrediction_CreatesSuccessfully()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var prediction = new MrSaturdayPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            Driver1Id = "hamilton",
            Driver2Id = "verstappen"
        };

        // Act
        var result = await _repository.UpsertMrSaturdayAsync(prediction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Driver1Id.Should().Be("hamilton");
        result.Driver2Id.Should().Be("verstappen");
    }

    [Fact]
    public async Task Upsert_ExistingPrediction_UpdatesSuccessfully()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var initialPrediction = new DestructorPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            Driver1Id = "mazepin",
            Driver2Id = "latifi"
        };
        await _repository.UpsertDestructorAsync(initialPrediction);
        
        // Clear change tracker to avoid tracking conflicts
        _context.ChangeTracker.Clear();

        // Act - Update with different drivers
        var updatedPrediction = new DestructorPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            Driver1Id = "stroll",
            Driver2Id = "tsunoda"
        };
        var result = await _repository.UpsertDestructorAsync(updatedPrediction);

        // Assert
        result.Driver1Id.Should().Be("stroll");
        result.Driver2Id.Should().Be("tsunoda");
        
        // Verify only one record exists
        _context.ChangeTracker.Clear();
        var all = await _repository.GetAllDestructorsAsync(_testGroupId);
        all.Count(p => p.UserId == _testUserId).Should().Be(1);
    }

    [Fact]
    public async Task GetAllPredictions_WithNoData_ReturnsEmptyObjects()
    {
        // Arrange
        await CleanupTestDataAsync();

        // Act
        var predictions = await _repository.GetAllPredictionsAsync(_testGroupId, "nonexistent_user");

        // Assert
        predictions.Should().NotBeNull();
        predictions.ConstructorChampionship.Should().BeNull();
        predictions.DriverChampionship.Should().BeNull();
        predictions.DriverDraft.Should().BeNull();
        predictions.Destructor.Should().BeNull();
        predictions.MrSaturday.Should().BeNull();
        predictions.ZeroPointer.Should().BeNull();
        predictions.Wildcard.Should().BeNull();
    }

    [Fact]
    public async Task GetAllWildcards_ReturnsAllWildcardsForGroup()
    {
        // Arrange
        await CleanupTestDataAsync();
        
        var wildcard = new WildcardPrediction
        {
            GroupId = _testGroupId,
            UserId = _testUserId,
            Statement = "Test wildcard",
            PointsPotential = 100
        };
        await _repository.UpsertWildcardAsync(wildcard);
        
        _context.ChangeTracker.Clear();

        // Act
        var wildcards = await _repository.GetAllWildcardsAsync(_testGroupId);

        // Assert
        wildcards.Should().NotBeEmpty();
        wildcards.Should().Contain(w => w.UserId == _testUserId);
        var testWildcard = wildcards.First(w => w.UserId == _testUserId);
        testWildcard.Statement.Should().Be("Test wildcard");
        testWildcard.PointsPotential.Should().Be(100);
    }

    private async Task CleanupTestDataAsync()
    {
        // Delete all test predictions
        var constructorPreds = await _context.ConstructorChampionshipPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.ConstructorChampionshipPredictions.RemoveRange(constructorPreds);

        var driverPreds = await _context.DriverChampionshipPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.DriverChampionshipPredictions.RemoveRange(driverPreds);

        var draftPreds = await _context.DriverDraftPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.DriverDraftPredictions.RemoveRange(draftPreds);

        var destructorPreds = await _context.DestructorPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.DestructorPredictions.RemoveRange(destructorPreds);

        var mrSatPreds = await _context.MrSaturdayPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.MrSaturdayPredictions.RemoveRange(mrSatPreds);

        var zeroPreds = await _context.ZeroPointerPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.ZeroPointerPredictions.RemoveRange(zeroPreds);

        var wildcardPreds = await _context.WildcardPredictions
            .Where(p => p.GroupId == _testGroupId)
            .ToListAsync();
        _context.WildcardPredictions.RemoveRange(wildcardPreds);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        try
        {
            CleanupTestDataAsync().Wait();
            
            // Clean up test group
            var testGroup = _context.Groups.Find(_testGroupId);
            if (testGroup != null)
            {
                _context.Groups.Remove(testGroup);
                _context.SaveChanges();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _context.Dispose();
        }
    }
}

// Helper class for creating DbContext instances in tests
public class TestDbContextFactory : IDbContextFactory<F1FantasyDbContext>
{
    private readonly DbContextOptions<F1FantasyDbContext> _options;

    public TestDbContextFactory(DbContextOptions<F1FantasyDbContext> options)
    {
        _options = options;
    }

    public F1FantasyDbContext CreateDbContext()
    {
        return new F1FantasyDbContext(_options);
    }

    public async Task<F1FantasyDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new F1FantasyDbContext(_options));
    }
}
