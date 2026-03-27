using F1Fantasy.Services;
using F1Fantasy.Repository;
using F1Fantasy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Investigation to identify which services have naive caching (returns stale data)
/// vs smart caching (checks for new data availability)
/// </summary>
[Collection("Sequential")]
public class CachingStalenessInvestigation : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private const string Season = "2026";

    public CachingStalenessInvestigation()
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
    public async Task Analyze_All_Service_Caching_Strategies()
    {
        Console.WriteLine("\n=== CACHING STRATEGY ANALYSIS ===\n");
        
        // DriverStandingService
        Console.WriteLine("1. DriverStandingService.GetDriverStandingsBySeasonCachedAsync()");
        Console.WriteLine("   Logic: CHECK IF ANY DATA EXISTS → RETURN (no staleness check)");
        Console.WriteLine("   Issue: ❌ NAIVE - Returns stale data without checking for new rounds");
        Console.WriteLine("   Evidence: Returned Round 1 standings when Round 2 was available");
        Console.WriteLine("");
        
        // ConstructorStandingService  
        Console.WriteLine("2. ConstructorStandingService.GetConstructorStandingsBySeasonCachedAsync()");
        Console.WriteLine("   Logic: CHECK IF ANY DATA EXISTS → RETURN (no staleness check)");
        Console.WriteLine("   Issue: ❌ NAIVE - Same pattern as DriverStandingService");
        Console.WriteLine("   Risk: HIGH - Will have same issue");
        Console.WriteLine("");
        
        // ResultService
        Console.WriteLine("3. ResultService.GetResultsBySeasonCachedAsync()");
        Console.WriteLine("   Logic: CHECK CACHE → RETURN (simple)");
        Console.WriteLine("   BUT: GetLatestRoundWithResultsAsync() has smart staleness detection:");
        Console.WriteLine("     - Checks metadata for last fetch time");
        Console.WriteLine("     - Checks race schedule for new races since last fetch");
        Console.WriteLine("     - Uses time-based expiration (1hr for current season, 7 days for past)");
        Console.WriteLine("   Status: ✅ SMART CACHING via GetLatestRoundWithResultsAsync()");
        Console.WriteLine("");
        
        // QualifyingService
        Console.WriteLine("4. QualifyingService.GetQualifyingBySeasonCachedAsync()");
        Console.WriteLine("   Checking implementation...");
        var qualifyingCode = await CheckQualifyingServiceCaching();
        Console.WriteLine($"   {qualifyingCode}");
        Console.WriteLine("");
        
        // Summary
        Console.WriteLine("\n=== SUMMARY ===");
        Console.WriteLine("❌ BROKEN (Naive Caching):");
        Console.WriteLine("   - DriverStandingService");
        Console.WriteLine("   - ConstructorStandingService");
        Console.WriteLine("   - Possibly QualifyingService (needs code review)");
        Console.WriteLine("");
        Console.WriteLine("✅ WORKING (Smart Caching):");
        Console.WriteLine("   - ResultService (via GetLatestRoundWithResultsAsync)");
        Console.WriteLine("");
        Console.WriteLine("🎯 RECOMMENDATION:");
        Console.WriteLine("   All services should follow ResultService pattern:");
        Console.WriteLine("   1. Check metadata for last fetch timestamp");
        Console.WriteLine("   2. Apply time-based expiration rules");
        Console.WriteLine("   3. Check if new rounds/races have occurred since last fetch");
        Console.WriteLine("   4. Only return cache if still valid");
    }

    private async Task<string> CheckQualifyingServiceCaching()
    {
        // Check if QualifyingService has similar naive caching
        // We'll do this by examining database metadata
        var qualifyingMetadata = await _context.DataFetchMetadata
            .FirstOrDefaultAsync(m => m.Season == Season && m.DataType == "Qualifying");
        
        if (qualifyingMetadata == null)
        {
            return "   Status: ⚠️ NO METADATA TRACKING - Cannot determine staleness";
        }
        
        var age = DateTime.UtcNow - qualifyingMetadata.LastFetchedAt;
        return $"   Last fetched: {qualifyingMetadata.LastFetchedAt}, Age: {age.TotalHours:F1} hours";
    }

    [Fact]
    public async Task Check_Metadata_Coverage()
    {
        Console.WriteLine("\n=== DATA FETCH METADATA COVERAGE ===\n");
        
        var allMetadata = await _context.DataFetchMetadata
            .Where(m => m.Season == Season)
            .ToListAsync();
        
        Console.WriteLine($"Metadata records for season {Season}:");
        foreach (var meta in allMetadata.OrderBy(m => m.DataType))
        {
            var age = DateTime.UtcNow - meta.LastFetchedAt;
            var status = meta.FetchSuccessful ? "✅" : "❌";
            Console.WriteLine($"{status} {meta.DataType,-20} | Last: {meta.LastFetchedAt:g} ({age.TotalHours:F1}h ago) | Round: {meta.LatestRoundAtFetch}");
        }
        
        // Check for missing metadata
        var expectedTypes = new[] { "Results", "Qualifying", "Races", "DriverStandings", "ConstructorStandings" };
        var existingTypes = allMetadata.Select(m => m.DataType).ToHashSet();
        
        Console.WriteLine("\n=== MISSING METADATA ===");
        foreach (var type in expectedTypes)
        {
            if (!existingTypes.Contains(type))
            {
                Console.WriteLine($"❌ {type} - NO METADATA TRACKING");
            }
        }
    }

    [Fact]
    public void Document_Ideal_Caching_Pattern()
    {
        Console.WriteLine("\n=== IDEAL CACHING PATTERN ===\n");
        Console.WriteLine(@"
public async Task<TData?> GetDataBySeasonCachedAsync(string season)
{
    // 1. Check if cache should be used
    if (!await ShouldFetchAsync(season))
    {
        // Cache is fresh, return it
        var cached = await BuildFromCache(season);
        if (cached != null && cached.Any())
        {
            _logger.LogInformation(""Using cached data for {Season}"", season);
            return cached;
        }
    }
    
    // 2. Cache is stale or empty, fetch from API
    _logger.LogInformation(""Fetching fresh data from API for {Season}"", season);
    return await GetDataBySeasonAsync(season);
}

private async Task<bool> ShouldFetchAsync(string season)
{
    // Check metadata for last fetch
    var metadata = await _metadataRepository.GetMetadataAsync(season, ""DataType"");
    
    if (metadata == null || !metadata.FetchSuccessful)
    {
        return true; // No valid cache
    }
    
    // Time-based expiration
    var currentYear = DateTime.UtcNow.Year;
    var seasonYear = int.Parse(season);
    var cacheExpiration = seasonYear < currentYear 
        ? TimeSpan.FromDays(7)  // Past seasons: 7 days
        : TimeSpan.FromHours(1); // Current season: 1 hour
    
    var age = DateTime.UtcNow - metadata.LastFetchedAt;
    if (age > cacheExpiration)
    {
        return true; // Cache expired
    }
    
    // Check for new data availability
    var races = await _raceRepository.GetBySeasonAsync(season);
    var racesSinceLastFetch = races
        .Where(r => DateTime.TryParse(r.Date, out var raceDate) && 
                   raceDate > metadata.LastFetchedAt &&
                   raceDate < DateTime.UtcNow.AddDays(1))
        .ToList();
    
    if (racesSinceLastFetch.Any())
    {
        return true; // New races have occurred
    }
    
    return false; // Cache is still valid
}
");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
