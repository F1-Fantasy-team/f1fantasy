using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Investigate driver draft calculation discrepancy
/// </summary>
[Collection("Sequential")]
public class DriverDraftInvestigationTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly DriverStandingRepository _driverStandingRepository;
    private readonly PredictionRepository _predictionRepository;
    private readonly GroupRepository _groupRepository;
    
    private readonly HttpClient _httpClient;
    private readonly DriverStandingService _driverStandingService;
    private readonly ScoringService _scoringService;
    private readonly ResultService _resultService;
    private readonly RaceService _raceService;
    private readonly QualifyingService _qualifyingService;
    private readonly ConstructorStandingService _constructorStandingService;
    
    private const string Season = "2026";

    public DriverDraftInvestigationTests()
    {
        var envPath = @"C:\Projects\f1fantasy\backend\.env";
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
        }
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Database connection string not found.");
        }

        var options = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        
        var contextFactory = new TestDbContextFactory(options);
        _context = contextFactory.CreateDbContext();
        
        _driverStandingRepository = new DriverStandingRepository(contextFactory.CreateDbContext(), NullLogger<DriverStandingRepository>.Instance);
        _groupRepository = new GroupRepository(contextFactory.CreateDbContext(), NullLogger<GroupRepository>.Instance);
        _predictionRepository = new PredictionRepository(contextFactory);
        
        var resultRepository = new ResultRepository(contextFactory.CreateDbContext(), NullLogger<ResultRepository>.Instance);
        var qualifyingRepository = new QualifyingRepository(contextFactory.CreateDbContext(), NullLogger<QualifyingRepository>.Instance);
        var constructorStandingRepository = new ConstructorStandingRepository(contextFactory.CreateDbContext(), NullLogger<ConstructorStandingRepository>.Instance);
        var metadataRepository = new DataFetchMetadataRepository(contextFactory.CreateDbContext(), NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepository = new RaceRepository(contextFactory.CreateDbContext(), NullLogger<RaceRepository>.Instance);
        
        var cacheStalenessService = new CacheStalenessService(metadataRepository, raceRepository, NullLogger<CacheStalenessService>.Instance);
        
        _httpClient = new HttpClient();
        _driverStandingService = new DriverStandingService(_httpClient, _driverStandingRepository, metadataRepository, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
        _resultService = new ResultService(_httpClient, resultRepository, metadataRepository, raceRepository, NullLogger<ResultService>.Instance);
        _raceService = new RaceService(_httpClient, raceRepository, metadataRepository, NullLogger<RaceService>.Instance);
        _qualifyingService = new QualifyingService(_httpClient, qualifyingRepository, metadataRepository, raceRepository, NullLogger<QualifyingService>.Instance);
        _constructorStandingService = new ConstructorStandingService(_httpClient, constructorStandingRepository, metadataRepository, raceRepository, NullLogger<ConstructorStandingService>.Instance);
        
        _scoringService = new ScoringService(
            _predictionRepository,
            _driverStandingService,
            _constructorStandingService,
            _resultService,
            _qualifyingService,
            _raceService);
    }

    [Fact]
    public async Task Investigate_Driver_Standings_Data()
    {
        // Get driver standings from database
        var standings = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(Season);
        
        Console.WriteLine($"\n=== Driver Standings for {Season} ===");
        Console.WriteLine($"Total drivers: {standings?.DriverStandings?.Count ?? 0}");
        
        if (standings?.DriverStandings != null)
        {
            var top10 = standings.DriverStandings.Take(10);
            foreach (var standing in top10)
            {
                Console.WriteLine($"P{standing.Position}: {standing.Driver?.DriverId} - {standing.Points} points");
            }
        }
        else
        {
            Console.WriteLine("NO DRIVER STANDINGS FOUND!");
        }
    }

    [Fact]
    public async Task Investigate_All_Member_Driver_Drafts()
    {
        var members = await _groupRepository.GetMembersAsync(58);
        var driverStandings = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(Season);
        
        Console.WriteLine($"\n=== Driver Draft Investigation for Group 58 ===");
        Console.WriteLine($"Driver Standings Available: {driverStandings != null}");
        Console.WriteLine($"Driver Standings Count: {driverStandings?.DriverStandings?.Count ?? 0}");
        
        foreach (var member in members)
        {
            var prediction = await _predictionRepository.GetDriverDraftAsync(58, member.UserId);
            if (prediction == null)
            {
                Console.WriteLine($"\nUser {member.UserId}: NO PREDICTION");
                continue;
            }
            
            Console.WriteLine($"\nUser {member.UserId}:");
            Console.WriteLine($"  Driver 1: {prediction.Driver1Id}");
            Console.WriteLine($"  Driver 2: {prediction.Driver2Id}");
            
            if (driverStandings?.DriverStandings != null)
            {
                var driver1Standing = driverStandings.DriverStandings.FirstOrDefault(s => s.Driver?.DriverId == prediction.Driver1Id);
                var driver2Standing = driverStandings.DriverStandings.FirstOrDefault(s => s.Driver?.DriverId == prediction.Driver2Id);
                
                Console.WriteLine($"  Driver 1 Points: {driver1Standing?.Points ?? "NOT FOUND"}");
                Console.WriteLine($"  Driver 2 Points: {driver2Standing?.Points ?? "NOT FOUND"}");
                
                int totalPoints = 0;
                if (driver1Standing != null && !string.IsNullOrEmpty(driver1Standing.Points))
                {
                    totalPoints += int.Parse(driver1Standing.Points);
                }
                if (driver2Standing != null && !string.IsNullOrEmpty(driver2Standing.Points))
                {
                    totalPoints += int.Parse(driver2Standing.Points);
                }
                
                Console.WriteLine($"  CALCULATED TOTAL: {totalPoints}");
            }
            
            // Also calculate using the service
            var serviceScore = await _scoringService.CalculateDriverDraftScoreAsync(58, member.UserId, Season);
            Console.WriteLine($"  SERVICE CALCULATED: {serviceScore}");
        }
    }

    [Fact]
    public async Task Check_If_Driver_Standings_Match_Results()
    {
        // Get driver standings
        var standings = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(Season);
        
        // Get results for both rounds
        var round1Results = await _context.Results.Where(r => r.Season == Season && r.Round == "1" && !r.IsSprint).ToListAsync();
        var round2Results = await _context.Results.Where(r => r.Season == Season && r.Round == "2" && !r.IsSprint).ToListAsync();
        
        Console.WriteLine($"\n=== Driver Standings vs Race Results ===");
        
        // Calculate expected points from race results
        var expectedPoints = new Dictionary<string, int>();
        foreach (var result in round1Results.Concat(round2Results))
        {
            if (!string.IsNullOrEmpty(result.DriverId) && !string.IsNullOrEmpty(result.Points))
            {
                if (!expectedPoints.ContainsKey(result.DriverId))
                    expectedPoints[result.DriverId] = 0;
                expectedPoints[result.DriverId] += int.Parse(result.Points);
            }
        }
        
        Console.WriteLine("\nTop 10 by Race Results:");
        var top10Expected = expectedPoints.OrderByDescending(kvp => kvp.Value).Take(10);
        foreach (var kvp in top10Expected)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value} points (from results)");
        }
        
        if (standings?.DriverStandings != null)
        {
            Console.WriteLine("\nTop 10 by Standings Table:");
            var top10Standings = standings.DriverStandings.Take(10);
            foreach (var standing in top10Standings)
            {
                var driverId = standing.Driver?.DriverId ?? "Unknown";
                Console.WriteLine($"  P{standing.Position}: {driverId}: {standing.Points} points (from standings)");
                
                if (expectedPoints.ContainsKey(driverId))
                {
                    var expected = expectedPoints[driverId];
                    var actual = int.Parse(standing.Points);
                    if (expected != actual)
                    {
                        Console.WriteLine($"    ⚠️ MISMATCH! Expected {expected} but standings show {actual}");
                    }
                }
            }
        }
    }

    [Fact]
    public async Task Compare_API_Data_With_Database_Standings()
    {
        Console.WriteLine($"\n=== API vs Database: Driver Standings ===");
        
        // Get from API directly (bypassing cache)
        var apiUrl = $"https://api.jolpi.ca/ergast/f1/{Season}/driverStandings.json";
        var apiResponse = await _httpClient.GetStringAsync(apiUrl);
        Console.WriteLine($"API Response received: {apiResponse.Length} characters");
        
        var apiData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(apiResponse);
        
        // Get from database
        var dbStandings = await _context.DriverStandings
            .Where(ds => ds.Season == Season)
            .OrderBy(ds => int.Parse(ds.Position))
            .Take(10)
            .ToListAsync();
        
        Console.WriteLine($"\nDatabase has {dbStandings.Count} driver standings for {Season}");
        Console.WriteLine("\nDatabase Top 10:");
        foreach (var standing in dbStandings)
        {
            Console.WriteLine($"  P{standing.Position}: {standing.DriverId} - {standing.Points} points");
        }
        
        Console.WriteLine("\n⚠️ Manual API comparison needed - check output above");
    }

    [Fact]
    public async Task Compare_API_Data_With_Database_Results()
    {
        Console.WriteLine($"\n=== API vs Database: Race Results ===");
        
        // Check Round 2 specifically
        var round = "2";
        var apiUrl = $"https://api.jolpi.ca/ergast/f1/{Season}/{round}/results.json";
        var apiResponse = await _httpClient.GetStringAsync(apiUrl);
        
        var apiData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(apiResponse);
        var races = apiData.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");
        
        Console.WriteLine($"\nAPI Round {round} Results:");
        if (races.GetArrayLength() > 0)
        {
            var race = races[0];
            var results = race.GetProperty("Results");
            Console.WriteLine($"Total drivers in API response: {results.GetArrayLength()}");
            
            for (int i = 0; i < Math.Min(10, results.GetArrayLength()); i++)
            {
                var result = results[i];
                var driverId = result.GetProperty("Driver").GetProperty("driverId").GetString();
                var position = result.GetProperty("position").GetString();
                var points = result.GetProperty("points").GetString();
                Console.WriteLine($"  P{position}: {driverId} - {points} points");
            }
        }
        
        // Get from database
        var dbResults = await _context.Results
            .Where(r => r.Season == Season && r.Round == round && !r.IsSprint)
            .OrderBy(r => int.Parse(r.Position))
            .Take(10)
            .ToListAsync();
        
        Console.WriteLine($"\nDatabase Round {round} Results:");
        Console.WriteLine($"Total drivers in database: {dbResults.Count}");
        foreach (var result in dbResults)
        {
            Console.WriteLine($"  P{result.Position}: {result.DriverId} - {result.Points} points");
        }
        
        // Compare
        Console.WriteLine("\n=== Comparison ===");
        if (races.GetArrayLength() > 0)
        {
            var results = races[0].GetProperty("Results");
            var apiDriverPoints = new Dictionary<string, string>();
            for (int i = 0; i < results.GetArrayLength(); i++)
            {
                var result = results[i];
                var driverId = result.GetProperty("Driver").GetProperty("driverId").GetString();
                var points = result.GetProperty("points").GetString();
                apiDriverPoints[driverId] = points;
            }
            
            foreach (var dbResult in dbResults)
            {
                if (apiDriverPoints.TryGetValue(dbResult.DriverId, out var apiPoints))
                {
                    if (dbResult.Points != apiPoints)
                    {
                        Console.WriteLine($"❌ MISMATCH: {dbResult.DriverId} - DB: {dbResult.Points}, API: {apiPoints}");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Match: {dbResult.DriverId} - {dbResult.Points} points");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Driver {dbResult.DriverId} in DB but not in API response");
                }
            }
        }
    }

    [Fact]
    public async Task Verify_Latest_Round_API_vs_Database()
    {
        Console.WriteLine($"\n=== Verifying Latest Round Detection ===");
        
        // Check what API says
        var apiUrl = $"https://api.jolpi.ca/ergast/f1/{Season}/results.json?limit=1000";
        var apiResponse = await _httpClient.GetStringAsync(apiUrl);
        var apiData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(apiResponse);
        
        var races = apiData.RootElement.GetProperty("MRData").GetProperty("RaceTable").GetProperty("Races");
        var apiRaceCount = races.GetArrayLength();
        
        Console.WriteLine($"API reports {apiRaceCount} races with results");
        
        if (apiRaceCount > 0)
        {
            var latestRace = races[apiRaceCount - 1];
            var latestRound = latestRace.GetProperty("round").GetString();
            Console.WriteLine($"API latest round: {latestRound}");
        }
        
        // Check what database says
        var dbLatestRound = await _context.Results
            .Where(r => r.Season == Season && !r.IsSprint)
            .Select(r => r.Round)
            .Distinct()
            .ToListAsync();
        
        var maxDbRound = dbLatestRound.Any() ? dbLatestRound.Max(r => int.Parse(r)) : 0;
        Console.WriteLine($"Database latest round: {maxDbRound}");
        Console.WriteLine($"Database has {dbLatestRound.Count} distinct rounds");
        
        // Check via service
        var serviceLatestRound = await _resultService.GetLatestRoundWithResultsAsync(Season);
        Console.WriteLine($"ResultService reports latest round: {serviceLatestRound}");
        
        if (apiRaceCount > 0)
        {
            var latestRace = races[apiRaceCount - 1];
            var apiLatestRound = int.Parse(latestRace.GetProperty("round").GetString());
            
            if (maxDbRound != apiLatestRound)
            {
                Console.WriteLine($"\n❌ CRITICAL: Database round ({maxDbRound}) does NOT match API round ({apiLatestRound})!");
            }
            else
            {
                Console.WriteLine($"\n✅ Database and API agree on latest round: {maxDbRound}");
            }
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _httpClient?.Dispose();
    }
}
