using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Diagnostic tests to investigate why round 2 data isn't being properly calculated
/// </summary>
[Collection("Sequential")]
public class Round2DataDiagnosticTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly ResultRepository _resultRepository;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly DriverStandingRepository _driverStandingRepository;
    private readonly ConstructorStandingRepository _constructorStandingRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly GroupRepository _groupRepository;
    private readonly PredictionRepository _predictionRepository;
    private readonly StandingRepository _standingRepository;
    
    private readonly HttpClient _httpClient;
    private readonly ResultService _resultService;
    private readonly RaceService _raceService;
    private readonly QualifyingService _qualifyingService;
    private readonly DriverStandingService _driverStandingService;
    private readonly ConstructorStandingService _constructorStandingService;
    private readonly ScoringService _scoringService;
    private readonly StandingsService _standingsService;
    
    private const string Season = "2026";

    public Round2DataDiagnosticTests()
    {
        // Load environment variables
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
        
        var contextFactory = new TestDbContextFactory(options);
        _context = contextFactory.CreateDbContext();
        
        // Initialize repositories
        _resultRepository = new ResultRepository(contextFactory.CreateDbContext(), NullLogger<ResultRepository>.Instance);
        _qualifyingRepository = new QualifyingRepository(contextFactory.CreateDbContext(), NullLogger<QualifyingRepository>.Instance);
        _driverStandingRepository = new DriverStandingRepository(contextFactory.CreateDbContext(), NullLogger<DriverStandingRepository>.Instance);
        _constructorStandingRepository = new ConstructorStandingRepository(contextFactory.CreateDbContext(), NullLogger<ConstructorStandingRepository>.Instance);
        _metadataRepository = new DataFetchMetadataRepository(contextFactory.CreateDbContext(), NullLogger<DataFetchMetadataRepository>.Instance);
        _raceRepository = new RaceRepository(contextFactory.CreateDbContext(), NullLogger<RaceRepository>.Instance);
        _groupRepository = new GroupRepository(contextFactory.CreateDbContext(), NullLogger<GroupRepository>.Instance);
        _predictionRepository = new PredictionRepository(contextFactory);
        _standingRepository = new StandingRepository(contextFactory.CreateDbContext());
        
        // Initialize HTTP client and services
        var cacheStalenessService = new CacheStalenessService(_metadataRepository, _raceRepository, NullLogger<CacheStalenessService>.Instance);
        
        _httpClient = new HttpClient();
        _resultService = new ResultService(_httpClient, _resultRepository, _metadataRepository, cacheStalenessService, NullLogger<ResultService>.Instance);
        _raceService = new RaceService(_httpClient, _raceRepository, _metadataRepository, cacheStalenessService, NullLogger<RaceService>.Instance);
        _qualifyingService = new QualifyingService(_httpClient, _qualifyingRepository, _metadataRepository, cacheStalenessService, NullLogger<QualifyingService>.Instance);
        _driverStandingService = new DriverStandingService(_httpClient, _driverStandingRepository, _metadataRepository, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
        _constructorStandingService = new ConstructorStandingService(_httpClient, _constructorStandingRepository, _metadataRepository, cacheStalenessService, NullLogger<ConstructorStandingService>.Instance);
        
        _scoringService = new ScoringService(
            _predictionRepository,
            _driverStandingService,
            _constructorStandingService,
            _resultService,
            _qualifyingService,
            _raceService);
        
        _standingsService = new StandingsService(
            _standingRepository,
            _groupRepository,
            _predictionRepository,
            _scoringService,
            _resultService,
            _resultRepository,
            _metadataRepository,
            NullLogger<StandingsService>.Instance);
    }

    [Fact]
    public async Task Test_Round2_Results_Are_In_Database()
    {
        // Arrange & Act
        var round1Results = await _resultRepository.GetByRaceAsync(Season, "1");
        var round2Results = await _resultRepository.GetByRaceAsync(Season, "2");
        
        // Assert
        Console.WriteLine($"Round 1 results count: {round1Results.Count()}");
        Console.WriteLine($"Round 2 results count: {round2Results.Count()}");
        
        round1Results.Should().NotBeEmpty("Round 1 should have results");
        round2Results.Should().NotBeEmpty("Round 2 should have results");
        
        // Print some round 2 data for verification
        foreach (var result in round2Results.Take(5))
        {
            Console.WriteLine($"Round 2 - Driver: {result.DriverId}, Position: {result.Position}, Points: {result.Points}");
        }
    }

    [Fact]
    public async Task Test_GetLatestRoundWithResults_Returns_Correct_Round()
    {
        // Act
        var latestRound = await _resultService.GetLatestRoundWithResultsAsync(Season);
        
        // Assert
        Console.WriteLine($"Latest round with results: {latestRound}");
        latestRound.Should().NotBeNull("There should be at least one round with results");
        latestRound.Should().BeGreaterOrEqualTo(2, "Round 2 should be completed");
    }

    [Fact]
    public async Task Test_Round2_Qualifying_Data_Exists()
    {
        // Act
        var round1Qualifying = await _qualifyingRepository.GetByRaceAsync(Season, "1");
        var round2Qualifying = await _qualifyingRepository.GetByRaceAsync(Season, "2");
        
        // Assert
        Console.WriteLine($"Round 1 qualifying count: {round1Qualifying.Count()}");
        Console.WriteLine($"Round 2 qualifying count: {round2Qualifying.Count()}");
        
        round1Qualifying.Should().NotBeEmpty("Round 1 should have qualifying data");
        
        // Round 2 qualifying might be missing - this is a known issue to investigate
        if (round2Qualifying.Any())
        {
            // Print some round 2 qualifying data
            foreach (var qual in round2Qualifying.Take(5))
            {
                Console.WriteLine($"Round 2 Quali - Driver: {qual.DriverId}, Position: {qual.Position}, Q3: {qual.Q3}");
            }
        }
        else
        {
            Console.WriteLine("WARNING: Round 2 qualifying data is missing - Mr Saturday scores will be 0 for round 2");
        }
    }

    [Fact]
    public async Task Test_ResultService_Returns_Both_Rounds()
    {
        // Act
        var allRaces = await _resultService.GetResultsBySeasonCachedAsync(Season);
        
        // Assert
        allRaces.Should().NotBeNull();
        var racesList = allRaces?.ToList() ?? new List<RaceWithResults>();
        
        Console.WriteLine($"Total races with results: {racesList.Count}");
        
        foreach (var race in racesList)
        {
            Console.WriteLine($"Round {race.Round}: {race.Results?.Count ?? 0} results");
        }
        
        var round1 = racesList.FirstOrDefault(r => r.Round == "1");
        var round2 = racesList.FirstOrDefault(r => r.Round == "2");
        
        round1.Should().NotBeNull("Round 1 should be in results");
        round2.Should().NotBeNull("Round 2 should be in results");
        
        round1?.Results.Should().NotBeNullOrEmpty("Round 1 should have results");
        round2?.Results.Should().NotBeNullOrEmpty("Round 2 should have results");
    }

    [Fact]
    public async Task Test_CalculateDetailedScores_Includes_Round2()
    {
        // Arrange - Get a real group from the database
        var group58 = await _groupRepository.GetByIdAsync(58);
        
        if (group58 == null)
        {
            Console.WriteLine("Group 58 not found - skipping test");
            return;
        }
        
        var members = await _groupRepository.GetMembersAsync(58);
        if (!members.Any())
        {
            Console.WriteLine("Group 58 has no members - skipping test");
            return;
        }
        
        var firstMember = members.First();
        
        // Act
        var detailedScores = await _scoringService.CalculateDetailedScoresAsync(58, firstMember.UserId, Season);
        
        // Assert
        Console.WriteLine($"User {firstMember.UserId} detailed scores:");
        Console.WriteLine($"Total Score: {detailedScores.TotalScore}");
        Console.WriteLine($"Round scores count: {detailedScores.RoundScores.Count}");
        
        foreach (var roundScore in detailedScores.RoundScores)
        {
            Console.WriteLine($"Round {roundScore.Round}: Cumulative Score = {roundScore.CumulativeScore}");
            foreach (var category in roundScore.CategoryScores)
            {
                if (category.Value != 0)
                {
                    Console.WriteLine($"  {category.Key}: {category.Value}");
                }
            }
        }
        
        detailedScores.RoundScores.Should().HaveCountGreaterOrEqualTo(2, "Should have scores for at least 2 rounds");
        
        var round1Score = detailedScores.RoundScores.FirstOrDefault(rs => rs.Round == "1");
        var round2Score = detailedScores.RoundScores.FirstOrDefault(rs => rs.Round == "2");
        
        round1Score.Should().NotBeNull("Round 1 should have scores");
        round2Score.Should().NotBeNull("Round 2 should have scores");
    }

    [Fact]
    public async Task Test_GetStandingsWithAutoRecalc_For_Group58()
    {
        // Act
        var standings = await _standingsService.GetStandingsWithAutoRecalcAsync(58, Season);
        
        // Assert
        Console.WriteLine($"Standings for Group 58:");
        foreach (var standing in standings)
        {
            Console.WriteLine($"User {standing.UserId}: Rank {standing.Rank}, Total Score: {standing.TotalScore}");
        }
        
        standings.Should().NotBeEmpty("Group 58 should have standings");
    }

    [Fact]
    public async Task Test_DataFetch_Metadata_For_Results()
    {
        // Act
        var metadata = await _metadataRepository.GetMetadataAsync(Season, "Results");
        
        // Assert
        if (metadata != null)
        {
            Console.WriteLine($"Results metadata for {Season}:");
            Console.WriteLine($"Last Fetched: {metadata.LastFetchedAt}");
            Console.WriteLine($"Fetch Successful: {metadata.FetchSuccessful}");
            Console.WriteLine($"Latest Round At Fetch: {metadata.LatestRoundAtFetch}");
            Console.WriteLine($"Error: {metadata.ErrorMessage ?? "None"}");
        }
        else
        {
            Console.WriteLine($"No metadata found for Results/{Season}");
        }
    }

    [Fact]
    public async Task Test_Compare_Round1_And_Round2_Scores()
    {
        // Arrange
        var group58 = await _groupRepository.GetByIdAsync(58);
        
        if (group58 == null)
        {
            Console.WriteLine("Group 58 not found - skipping test");
            return;
        }
        
        var members = await _groupRepository.GetMembersAsync(58);
        if (!members.Any())
        {
            Console.WriteLine("Group 58 has no members - skipping test");
            return;
        }
        
        // Act - Calculate scores for first member
        var firstMember = members.First();
        var detailedScores = await _scoringService.CalculateDetailedScoresAsync(58, firstMember.UserId, Season);
        
        // Assert
        Console.WriteLine($"\n=== Score Comparison for User {firstMember.UserId} ===");
        
        var round1 = detailedScores.RoundScores.FirstOrDefault(rs => rs.Round == "1");
        var round2 = detailedScores.RoundScores.FirstOrDefault(rs => rs.Round == "2");
        
        if (round1 != null)
        {
            Console.WriteLine($"\nRound 1 Scores:");
            foreach (var cat in round1.CategoryScores.Where(c => c.Value != 0))
            {
                Console.WriteLine($"  {cat.Key}: {cat.Value}");
            }
            Console.WriteLine($"  Cumulative: {round1.CumulativeScore}");
        }
        
        if (round2 != null)
        {
            Console.WriteLine($"\nRound 2 Scores:");
            foreach (var cat in round2.CategoryScores.Where(c => c.Value != 0))
            {
                Console.WriteLine($"  {cat.Key}: {cat.Value}");
            }
            Console.WriteLine($"  Cumulative: {round2.CumulativeScore}");
        }
        
        if (round1 != null && round2 != null)
        {
            var scoreDifference = round2.CumulativeScore - round1.CumulativeScore;
            Console.WriteLine($"\nScore increase from Round 1 to Round 2: {scoreDifference}");
            
            // The cumulative score should increase from round 1 to round 2 (or at least not decrease significantly)
            scoreDifference.Should().BeGreaterOrEqualTo(0, "Cumulative score should not decrease between rounds");
        }
    }

    [Fact]
    public async Task Test_Recalculation_Detection_Logic()
    {
        // This test verifies the logic that determines if recalculation is needed
        
        // Act
        var latestRound = await _resultService.GetLatestRoundWithResultsAsync(Season);
        var existingStandings = await _standingRepository.GetStandingsByGroupAsync(58);
        
        Console.WriteLine($"Latest round with results: {latestRound}");
        Console.WriteLine($"Existing standings count: {existingStandings.Count}");
        
        if (existingStandings.Any() && latestRound.HasValue)
        {
            // Try to determine last calculated round from first user
            var firstStanding = existingStandings.First();
            var detailedStanding = await _scoringService.CalculateDetailedScoresAsync(58, firstStanding.UserId, Season);
            
            int? lastCalculatedRound = null;
            if (detailedStanding.RoundScores.Any())
            {
                lastCalculatedRound = detailedStanding.RoundScores.Max(rs => int.Parse(rs.Round));
                Console.WriteLine($"Last calculated round: {lastCalculatedRound}");
            }
            
            bool needsRecalc = lastCalculatedRound == null || lastCalculatedRound < latestRound;
            Console.WriteLine($"Needs recalculation: {needsRecalc}");
            Console.WriteLine($"Reason: lastCalculatedRound({lastCalculatedRound}) < latestRound({latestRound})");
        }
    }

    [Fact]
    public async Task Test_All_Members_Round2_Scores()
    {
        // Get all members of group 58 and check their round 2 scores
        var members = await _groupRepository.GetMembersAsync(58);
        
        Console.WriteLine($"\n=== All Members Round 2 Scores ===");
        
        foreach (var member in members)
        {
            var detailedScores = await _scoringService.CalculateDetailedScoresAsync(58, member.UserId, Season);
            var round2 = detailedScores.RoundScores.FirstOrDefault(rs => rs.Round == "2");
            
            Console.WriteLine($"\nUser: {member.UserId}");
            if (round2 != null)
            {
                foreach (var cat in round2.CategoryScores.Where(c => c.Value != 0))
                {
                    Console.WriteLine($"  {cat.Key}: {cat.Value}");
                }
                Console.WriteLine($"  Cumulative: {round2.CumulativeScore}");
            }
            else
            {
                Console.WriteLine("  No Round 2 scores found!");
            }
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
        _httpClient?.Dispose();
    }
}
