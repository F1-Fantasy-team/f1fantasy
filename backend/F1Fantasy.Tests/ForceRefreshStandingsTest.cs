using F1Fantasy.Data;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Force refresh standings to fix stale data issue
/// </summary>
[Collection("Sequential")]
public class ForceRefreshStandingsTest : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly DriverStandingService _driverStandingService;
    private readonly ConstructorStandingService _constructorStandingService;
    
    private const string Season = "2026";

    public ForceRefreshStandingsTest()
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
        
        var contextFactory = new TestDbContextFactory(options);
        _context = contextFactory.CreateDbContext();
        
        var driverStandingRepo = new DriverStandingRepository(contextFactory.CreateDbContext(), NullLogger<DriverStandingRepository>.Instance);
        var constructorStandingRepo = new ConstructorStandingRepository(contextFactory.CreateDbContext(), NullLogger<ConstructorStandingRepository>.Instance);
        var metadataRepository = new DataFetchMetadataRepository(contextFactory.CreateDbContext(), NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepository = new RaceRepository(contextFactory.CreateDbContext(), NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepository, raceRepository, NullLogger<CacheStalenessService>.Instance);
        
        var httpClient = new HttpClient();
        _driverStandingService = new DriverStandingService(httpClient, driverStandingRepo, metadataRepository, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
        _constructorStandingService = new ConstructorStandingService(httpClient, constructorStandingRepo, metadataRepository, cacheStalenessService, NullLogger<ConstructorStandingService>.Instance);
    }

    [Fact]
    public async Task Force_Refresh_Driver_Standings()
    {
        Console.WriteLine($"\n=== BEFORE Refresh ===");
        var beforeStandings = await _context.DriverStandings
            .Where(ds => ds.Season == Season)
            .OrderBy(ds => ds.Position)
            .Take(5)
            .ToListAsync();
        
        foreach (var s in beforeStandings)
        {
            Console.WriteLine($"P{s.Position}: {s.DriverId} - {s.Points} points");
        }
        
        // Force fetch from API (bypasses cache)
        Console.WriteLine($"\n=== Fetching from API ===");
        var freshStandings = await _driverStandingService.GetDriverStandingsBySeasonAsync(Season);
        
        Console.WriteLine($"\n=== AFTER Refresh ===");
        var afterStandings = await _context.DriverStandings
            .Where(ds => ds.Season == Season)
            .OrderBy(ds => ds.Position)
            .Take(5)
            .ToListAsync();
        
        foreach (var s in afterStandings)
        {
            Console.WriteLine($"P{s.Position}: {s.DriverId} - {s.Points} points");
        }
        
        Console.WriteLine($"\n✅ Driver standings refreshed from API");
    }

    [Fact]
    public async Task Force_Refresh_Constructor_Standings()
    {
        Console.WriteLine($"\n=== BEFORE Refresh ===");
        var beforeStandings = await _context.ConstructorStandings
            .Where(cs => cs.Season == Season)
            .OrderBy(cs => cs.Position)
            .Take(5)
            .ToListAsync();
        
        foreach (var s in beforeStandings)
        {
            Console.WriteLine($"P{s.Position}: {s.ConstructorId} - {s.Points} points");
        }
        
        // Force fetch from API (bypasses cache)
        Console.WriteLine($"\n=== Fetching from API ===");
        var freshStandings = await _constructorStandingService.GetConstructorStandingsBySeasonAsync(Season);
        
        Console.WriteLine($"\n=== AFTER Refresh ===");
        var afterStandings = await _context.ConstructorStandings
            .Where(cs => cs.Season == Season)
            .OrderBy(cs => cs.Position)
            .Take(5)
            .ToListAsync();
        
        foreach (var s in afterStandings)
        {
            Console.WriteLine($"P{s.Position}: {s.ConstructorId} - {s.Points} points");
        }
        
        Console.WriteLine($"\n✅ Constructor standings refreshed from API");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
