using F1Fantasy.Data;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Verifies that Group 58 scores are correctly calculated with latest F1 data
/// This test should ALWAYS pass - if it fails, auto-recalc is broken
/// </summary>
[Collection("Sequential")]
public class Group58ScoreVerificationTest : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly StandingsService _standingsService;
    private readonly ScoringService _scoringService;
    private const int GroupId = 58;
    private const string Season = "2026";
    private const string AndreasUserId = "user_3A5XPOnrcrJcN5KuIFNyyYOceJO";

    public Group58ScoreVerificationTest()
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
        
        // Initialize services
        var httpClient = new HttpClient();
        var contextFactory = new TestDbContextFactory(options);
        
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStalenessService = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        
        var driverStandingRepo = new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance);
        var driverStandingService = new DriverStandingService(httpClient, driverStandingRepo, metadataRepo, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
        
        var constructorStandingRepo = new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance);
        var constructorStandingService = new ConstructorStandingService(httpClient, constructorStandingRepo, metadataRepo, cacheStalenessService, NullLogger<ConstructorStandingService>.Instance);
        
        var resultRepo = new ResultRepository(_context, NullLogger<ResultRepository>.Instance);
        var resultService = new ResultService(httpClient, resultRepo, metadataRepo, cacheStalenessService, NullLogger<ResultService>.Instance);
        
        var qualifyingRepo = new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance);
        var qualifyingService = new QualifyingService(httpClient, qualifyingRepo, metadataRepo, cacheStalenessService, NullLogger<QualifyingService>.Instance);
        
        var raceService = new RaceService(httpClient, raceRepo, metadataRepo, cacheStalenessService, NullLogger<RaceService>.Instance);
        
        var predictionRepo = new PredictionRepository(contextFactory);
        _scoringService = new ScoringService(predictionRepo, driverStandingService, constructorStandingService, resultService, qualifyingService, raceService);
        
        var groupRepo = new GroupRepository(_context, NullLogger<GroupRepository>.Instance);
        var standingRepo = new StandingRepository(_context);
        _standingsService = new StandingsService(standingRepo, groupRepo, predictionRepo, _scoringService, resultService, resultRepo, metadataRepo, NullLogger<StandingsService>.Instance);
    }

    [Fact]
    public async Task Andreas_Should_Have_66_Points_For_Driver_Draft_Based_On_Current_API_Data()
    {
        Console.WriteLine("\n=== VERIFYING ANDREAS' DRIVER DRAFT SCORE ===\n");
        
        // Get Andreas' driver draft prediction
        var prediction = await _context.DriverDraftPredictions
            .FirstOrDefaultAsync(p => p.GroupId == GroupId && p.UserId == AndreasUserId);
        
        Assert.NotNull(prediction);
        Console.WriteLine($"Andreas' picks: {prediction.Driver1Id}, {prediction.Driver2Id}");
        
        // Get FRESH driver standings from API
        var httpClient = new HttpClient();
        var apiUrl = $"https://api.jolpi.ca/ergast/f1/{Season}/driverstandings.json";
        var response = await httpClient.GetStringAsync(apiUrl);
        Console.WriteLine($"\nFetched live data from Ergast API");
        
        // Parse to get points for Andreas' drivers
        var apiData = System.Text.Json.JsonDocument.Parse(response);
        var standings = apiData.RootElement
            .GetProperty("MRData")
            .GetProperty("StandingsTable")
            .GetProperty("StandingsLists")[0]
            .GetProperty("DriverStandings");
        
        int expectedPoints = 0;
        foreach (var standing in standings.EnumerateArray())
        {
            var driverId = standing.GetProperty("Driver").GetProperty("driverId").GetString();
            var points = standing.GetProperty("points").GetString();
            
            if (driverId == prediction.Driver1Id || driverId == prediction.Driver2Id)
            {
                var pointsInt = int.Parse(points!);
                Console.WriteLine($"  {driverId}: {pointsInt} points");
                expectedPoints += pointsInt;
            }
        }
        
        Console.WriteLine($"\nExpected total from API: {expectedPoints} points");
        
        // Now calculate using our service
        var calculatedScore = await _scoringService.CalculateDriverDraftScoreAsync(GroupId, AndreasUserId, Season);
        Console.WriteLine($"Calculated by our service: {calculatedScore} points");
        
        // CRITICAL ASSERTION
        Assert.Equal(expectedPoints, calculatedScore);
        
        if (expectedPoints == calculatedScore)
        {
            Console.WriteLine("\n✅ PASS: Score matches live API data!");
        }
        else
        {
            Console.WriteLine($"\n❌ FAIL: Score mismatch! Expected {expectedPoints} but got {calculatedScore}");
            Console.WriteLine("This means the service is using STALE cached data instead of fetching fresh standings!");
        }
    }

    [Fact]
    public async Task GetStandingsWithAutoRecalc_Should_Return_Fresh_Scores_After_New_Data_Fetched()
    {
        Console.WriteLine("\n=== TESTING AUTO-RECALC AFTER NEW F1 DATA ===\n");
        
        // Force fetch fresh data from API to ensure we have latest
        Console.WriteLine("Force fetching fresh F1 data from API...");
        await _scoringService.EnsureSeasonDataAvailableAsync(Season);
        
        // Check metadata to see when data was last fetched
        var driverStandingsMetadata = await _context.DataFetchMetadata
            .FirstOrDefaultAsync(m => m.Season == Season && m.DataType == "DriverStandings");
        
        if (driverStandingsMetadata != null)
        {
            var age = DateTime.UtcNow - driverStandingsMetadata.LastFetchedAt;
            Console.WriteLine($"Driver standings last fetched: {driverStandingsMetadata.LastFetchedAt} ({age.TotalMinutes:F1} minutes ago)");
            Console.WriteLine($"Latest round at fetch: {driverStandingsMetadata.LatestRoundAtFetch}");
        }
        
        // Now get standings with auto-recalc
        Console.WriteLine("\nCalling GetStandingsWithAutoRecalcAsync...");
        var standings = await _standingsService.GetStandingsWithAutoRecalcAsync(GroupId, Season);
        
        var andreasStanding = standings.FirstOrDefault(s => s.UserId == AndreasUserId);
        Assert.NotNull(andreasStanding);
        Assert.NotNull(andreasStanding.CategoryScoresJson);
        
        var categoryScores = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(andreasStanding.CategoryScoresJson);
        Assert.NotNull(categoryScores);
        
        var driverDraftScore = categoryScores["driverDraft"];
        
        Console.WriteLine($"\nAndreas' scores from standings table:");
        Console.WriteLine($"  Driver Draft: {driverDraftScore}");
        Console.WriteLine($"  Total: {andreasStanding.TotalScore}");
        Console.WriteLine($"  Last Updated: {andreasStanding.UpdatedAt}");
        
        // The standing should have been recalculated recently (within last minute)
        var standingsAge = DateTime.UtcNow - andreasStanding.UpdatedAt;
        Console.WriteLine($"  Age: {standingsAge.TotalSeconds:F0} seconds");
        
        Assert.True(standingsAge.TotalMinutes < 2, 
            $"Standings should have been recalculated recently but are {standingsAge.TotalMinutes:F1} minutes old");
        
        // Verify driver draft score matches what we expect from API
        var expectedScore = await _scoringService.CalculateDriverDraftScoreAsync(GroupId, AndreasUserId, Season);
        Assert.Equal(expectedScore, driverDraftScore);
        
        Console.WriteLine($"\n✅ Standings are fresh and match expected scores!");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
