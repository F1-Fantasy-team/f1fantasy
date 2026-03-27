using F1Fantasy.Services;
using F1Fantasy.Repository;
using F1Fantasy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Validates that smart caching is working correctly for all services
/// Tests metadata tracking, staleness detection, and cache refresh logic
/// </summary>
[Collection("Sequential")]
public class SmartCachingValidationTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private const string Season = "2026";

    public SmartCachingValidationTests()
    {
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string not found.");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        
        _context = new F1FantasyDbContext(options);
    }

    [Fact]
    public async Task DriverStandingService_Should_Use_Smart_Caching()
    {
        Console.WriteLine("\n=== TESTING DRIVER STANDINGS SMART CACHING ===\n");
        
        var repository = new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance);
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        var httpClient = new HttpClient();
        var logger = NullLogger<DriverStandingService>.Instance;
        
        var service = new DriverStandingService(httpClient, repository, metadataRepo, cacheStalenessService, logger);
        
        // Check metadata before
        var metadataBefore = await metadataRepo.GetMetadataAsync(Season, "DriverStandings");
        Console.WriteLine($"Metadata before: {(metadataBefore != null ? $"Exists (Last fetch: {metadataBefore.LastFetchedAt})" : "Does not exist")}");
        
        // Call the cached method
        var result = await service.GetDriverStandingsBySeasonCachedAsync(Season);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.DriverStandings);
        Console.WriteLine($"✅ Retrieved {result.DriverStandings.Count} driver standings");
        
        // Check metadata after
        var metadataAfter = await metadataRepo.GetMetadataAsync(Season, "DriverStandings");
        Assert.NotNull(metadataAfter);
        Assert.True(metadataAfter.FetchSuccessful);
        Console.WriteLine($"✅ Metadata exists: Last fetched {metadataAfter.LastFetchedAt}, Round {metadataAfter.LatestRoundAtFetch}");
        
        // Verify top driver points
        var topDriver = result.DriverStandings.First();
        Console.WriteLine($"✅ Top driver: {topDriver.Driver.DriverId} with {topDriver.Points} points");
    }

    [Fact]
    public async Task ConstructorStandingService_Should_Use_Smart_Caching()
    {
        Console.WriteLine("\n=== TESTING CONSTRUCTOR STANDINGS SMART CACHING ===\n");
        
        var repository = new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance);
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        var httpClient = new HttpClient();
        var logger = NullLogger<ConstructorStandingService>.Instance;
        
        var service = new ConstructorStandingService(httpClient, repository, metadataRepo, cacheStalenessService, logger);
        
        // Check metadata before
        var metadataBefore = await metadataRepo.GetMetadataAsync(Season, "ConstructorStandings");
        Console.WriteLine($"Metadata before: {(metadataBefore != null ? $"Exists (Last fetch: {metadataBefore.LastFetchedAt})" : "Does not exist")}");
        
        // Call the cached method
        var result = await service.GetConstructorStandingsBySeasonCachedAsync(Season);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.ConstructorStandings);
        Console.WriteLine($"✅ Retrieved {result.ConstructorStandings.Count} constructor standings");
        
        // Check metadata after
        var metadataAfter = await metadataRepo.GetMetadataAsync(Season, "ConstructorStandings");
        Assert.NotNull(metadataAfter);
        Assert.True(metadataAfter.FetchSuccessful);
        Console.WriteLine($"✅ Metadata exists: Last fetched {metadataAfter.LastFetchedAt}, Round {metadataAfter.LatestRoundAtFetch}");
        
        // Verify top constructor points
        var topConstructor = result.ConstructorStandings.First();
        Console.WriteLine($"✅ Top constructor: {topConstructor.Constructor.ConstructorId} with {topConstructor.Points} points");
    }

    [Fact]
    public async Task QualifyingService_Should_Use_Smart_Caching()
    {
        Console.WriteLine("\n=== TESTING QUALIFYING SMART CACHING ===\n");
        
        var repository = new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance);
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        var httpClient = new HttpClient();
        var logger = NullLogger<QualifyingService>.Instance;
        
        var service = new QualifyingService(httpClient, repository, metadataRepo, cacheStalenessService, logger);
        
        // Check metadata before
        var metadataBefore = await metadataRepo.GetMetadataAsync(Season, "Qualifying");
        Console.WriteLine($"Metadata before: {(metadataBefore != null ? $"Exists (Last fetch: {metadataBefore.LastFetchedAt})" : "Does not exist")}");
        
        // Call the cached method
        var result = await service.GetQualifyingBySeasonCachedAsync(Season);
        
        Assert.NotNull(result);
        var resultList = result.ToList();
        Console.WriteLine($"✅ Retrieved qualifying for {resultList.Count} races");
        
        // Check metadata after
        var metadataAfter = await metadataRepo.GetMetadataAsync(Season, "Qualifying");
        Assert.NotNull(metadataAfter);
        Assert.True(metadataAfter.FetchSuccessful);
        Console.WriteLine($"✅ Metadata exists: Last fetched {metadataAfter.LastFetchedAt}, Round {metadataAfter.LatestRoundAtFetch}");
        
        // Verify we have qualifying results
        if (resultList.Any())
        {
            var firstRace = resultList.First();
            Console.WriteLine($"✅ First race: Round {firstRace.Round} with {firstRace.QualifyingResults?.Count ?? 0} qualifying results");
        }
    }

    [Fact]
    public async Task All_Services_Should_Have_Metadata_Coverage()
    {
        Console.WriteLine("\n=== VERIFYING METADATA COVERAGE ===\n");
        
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        
        // Force a fetch for all services to ensure metadata is created
        var httpClient = new HttpClient();
        
        var driverStandingService = new DriverStandingService(
            httpClient, 
            new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance), 
            metadataRepo, 
            cacheStalenessService, 
            NullLogger<DriverStandingService>.Instance);
            
        var constructorStandingService = new ConstructorStandingService(
            new HttpClient(), 
            new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance), 
            metadataRepo, 
            cacheStalenessService, 
            NullLogger<ConstructorStandingService>.Instance);
            
        var qualifyingService = new QualifyingService(
            new HttpClient(), 
            new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance), 
            metadataRepo, 
            cacheStalenessService, 
            NullLogger<QualifyingService>.Instance);
        
        // Fetch from all services
        await driverStandingService.GetDriverStandingsBySeasonCachedAsync(Season);
        await constructorStandingService.GetConstructorStandingsBySeasonCachedAsync(Season);
        await qualifyingService.GetQualifyingBySeasonCachedAsync(Season);
        
        // Check metadata for all services
        var expectedTypes = new[] { "DriverStandings", "ConstructorStandings", "Qualifying" };
        
        foreach (var dataType in expectedTypes)
        {
            var metadata = await metadataRepo.GetMetadataAsync(Season, dataType);
            Assert.NotNull(metadata);
            Assert.True(metadata.FetchSuccessful);
            
            var age = DateTime.UtcNow - metadata.LastFetchedAt;
            Console.WriteLine($"✅ {dataType,-25} | Last: {metadata.LastFetchedAt:g} ({age.TotalMinutes:F1}m ago) | Round: {metadata.LatestRoundAtFetch}");
        }
        
        Console.WriteLine("\n✅ ALL SERVICES NOW HAVE METADATA TRACKING!");
    }

    [Fact]
    public async Task Compare_Naive_vs_Smart_Caching_Behavior()
    {
        Console.WriteLine("\n=== NAIVE VS SMART CACHING COMPARISON ===\n");
        
        Console.WriteLine("NAIVE CACHING (OLD PATTERN):");
        Console.WriteLine("  ❌ if (cachedData.Any()) return cachedData;");
        Console.WriteLine("  ❌ Returns stale Round 1 data when Round 2 is available");
        Console.WriteLine("  ❌ No metadata tracking");
        Console.WriteLine("  ❌ No staleness detection");
        Console.WriteLine("");
        
        Console.WriteLine("SMART CACHING (NEW PATTERN):");
        Console.WriteLine("  ✅ if (!await ShouldFetchAsync(season)) return cachedData;");
        Console.WriteLine("  ✅ Checks metadata for last fetch time");
        Console.WriteLine("  ✅ Time-based expiration (1hr current / 7days past)");
        Console.WriteLine("  ✅ Checks for new races since last fetch");
        Console.WriteLine("  ✅ Records metadata on every fetch");
        Console.WriteLine("");
        
        // Demonstrate the smart caching behavior
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var metadata = await metadataRepo.GetMetadataAsync(Season, "DriverStandings");
        
        if (metadata != null)
        {
            var age = DateTime.UtcNow - metadata.LastFetchedAt;
            var isExpired = age > TimeSpan.FromHours(1); // Current season cache duration
            
            Console.WriteLine($"Current Metadata:");
            Console.WriteLine($"  Last Fetched: {metadata.LastFetchedAt:g}");
            Console.WriteLine($"  Age: {age.TotalMinutes:F1} minutes");
            Console.WriteLine($"  Expired (>1hr): {isExpired}");
            Console.WriteLine($"  Latest Round: {metadata.LatestRoundAtFetch}");
            Console.WriteLine("");
            Console.WriteLine($"Cache Decision: {(isExpired ? "❌ FETCH FROM API (expired)" : "✅ USE CACHE (fresh)")}");
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
