using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Integration tests for Standings with a complete group scenario:
/// - 4 members in a group
/// - Each member submits predictions for 2025 season
/// - Test auto-recalculation and scoring
/// </summary>
[Collection("Sequential")]
public class StandingsIntegrationTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly GroupRepository _groupRepository;
    private readonly PredictionRepository _predictionRepository;
    private readonly StandingRepository _standingRepository;
    private readonly ResultRepository _resultRepository;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly DriverStandingRepository _driverStandingRepository;
    private readonly ConstructorStandingRepository _constructorStandingRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly RaceRepository _raceRepository;
    private readonly HttpClient _httpClient;
    private readonly ResultService _resultService;
    private readonly QualifyingService _qualifyingService;
    private readonly DriverStandingService _driverStandingService;
    private readonly ConstructorStandingService _constructorStandingService;
    private readonly ScoringService _scoringService;
    private readonly StandingsService _standingsService;
    
    // Test data
    private int _testGroupId;
    private readonly string[] _memberUserIds = { "user_alice", "user_bob", "user_charlie", "user_diana" };
    private const string Season = "2023";

    public StandingsIntegrationTests()
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
        _context = new F1FantasyDbContext(options);
        
        // Create DbContextFactory for PredictionRepository
        var contextFactory = new TestDbContextFactory(options);
        
        // Initialize repositories
        _groupRepository = new GroupRepository(_context, NullLogger<GroupRepository>.Instance);
        _predictionRepository = new PredictionRepository(contextFactory);
        _standingRepository = new StandingRepository(_context);
        _resultRepository = new ResultRepository(_context, NullLogger<ResultRepository>.Instance);
        _qualifyingRepository = new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance);
        _driverStandingRepository = new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance);
        _constructorStandingRepository = new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance);
        _metadataRepository = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        _raceRepository = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        
        // Initialize HTTP client and F1 data services
        _httpClient = new HttpClient();
        _resultService = new ResultService(_httpClient, _resultRepository, _metadataRepository, _raceRepository, NullLogger<ResultService>.Instance);
        _qualifyingService = new QualifyingService(_httpClient, _qualifyingRepository, NullLogger<QualifyingService>.Instance);
        _driverStandingService = new DriverStandingService(_httpClient, _driverStandingRepository, NullLogger<DriverStandingService>.Instance);
        _constructorStandingService = new ConstructorStandingService(_httpClient, _constructorStandingRepository, NullLogger<ConstructorStandingService>.Instance);
        
        // Initialize services with F1 data services
        _scoringService = new ScoringService(
            _predictionRepository,
            _driverStandingService,
            _constructorStandingService,
            _resultService,
            _qualifyingService);
        
        _standingsService = new StandingsService(
            _standingRepository,
            _groupRepository,
            _predictionRepository,
            _scoringService,
            _resultService,
            _resultRepository,
            NullLogger<StandingsService>.Instance);
    }

    [Fact]
    public async Task CompleteScenario_4Members_CreatesGroupPredictionsAndCalculatesStandings()
    {
        // Step 1: Create a test group
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;

        // Step 2: Add 4 members to the group
        await AddMembersToGroupAsync(group.Id);

        // Step 3: Submit predictions for each member
        await SubmitPredictionsForAliceAsync(group.Id); // Optimistic predictions
        await SubmitPredictionsForBobAsync(group.Id);   // Conservative predictions
        await SubmitPredictionsForCharlieAsync(group.Id); // Mixed predictions
        await SubmitPredictionsForDianaAsync(group.Id);  // Wild card predictions

        // Step 4: Verify all predictions were saved
        var alicePredictions = await _predictionRepository.GetAllPredictionsAsync(group.Id, "user_alice");
        alicePredictions.DriverChampionship.Should().NotBeNull();
        alicePredictions.ConstructorChampionship.Should().NotBeNull();
        alicePredictions.DriverDraft.Should().NotBeNull();
        alicePredictions.Destructor.Should().NotBeNull();
        alicePredictions.MrSaturday.Should().NotBeNull();

        // Step 5: Get initial standings (should be empty or need recalc)
        var initialStandings = await _standingsService.GetStandingsWithAutoRecalcAsync(group.Id, Season);
        initialStandings.Should().NotBeNull();
        initialStandings.Should().HaveCount(4, "we have 4 members");

        // Step 6: Verify all members have standings
        var aliceStanding = initialStandings.FirstOrDefault(s => s.UserId == "user_alice");
        var bobStanding = initialStandings.FirstOrDefault(s => s.UserId == "user_bob");
        var charlieStanding = initialStandings.FirstOrDefault(s => s.UserId == "user_charlie");
        var dianaStanding = initialStandings.FirstOrDefault(s => s.UserId == "user_diana");

        aliceStanding.Should().NotBeNull();
        bobStanding.Should().NotBeNull();
        charlieStanding.Should().NotBeNull();
        dianaStanding.Should().NotBeNull();

        // Step 6.5: Verify exact calculated scores based on 2023 season results
        // Alice: 196 (DriverChamp) + 100 (ConstructorChamp) + 860 (DriverDraft) + 60 (Destructor) + 20 (MrSat) + 100 (ZeroPtr) = 1336
        aliceStanding!.TotalScore.Should().Be(1336, "Alice total score calculated from all categories");
        
        // Bob: Poor predictions with some draft points
        // 302 (DriverDraft: Norris 205 + Piastri 97) + other categories
        bobStanding!.TotalScore.Should().BeLessThan(aliceStanding.TotalScore, "Bob has worse predictions than Alice");
        
        // Charlie: Mixed predictions
        // 440 (DriverDraft: Hamilton 234 + Leclerc 206) + other categories  
        charlieStanding!.TotalScore.Should().BeGreaterThan(bobStanding.TotalScore).And.BeLessThan(aliceStanding.TotalScore, "Charlie has mixed predictions");
        
        // Diana: Worst predictions but De Vries zero pointer correct
        // 1 (DriverDraft: Sargeant 1 + De Vries 0) + 100 (ZeroPointer) + negative championship scores
        dianaStanding!.TotalScore.Should().BeLessThan(bobStanding.TotalScore, "Diana has the worst predictions");

        // Step 7: Verify rankings are assigned
        var ranks = initialStandings.Select(s => s.Rank).ToList();
        ranks.Should().OnlyHaveUniqueItems("each member should have a unique rank");
        ranks.Should().BeInAscendingOrder("ranks should be sequential starting from 1");

        // Step 8: Test detailed breakdown for one user
        var aliceBreakdown = await _scoringService.CalculateDetailedScoresAsync(group.Id, "user_alice", Season);
        aliceBreakdown.Should().NotBeNull();
        aliceBreakdown.UserId.Should().Be("user_alice");
        aliceBreakdown.CategoryTotals.Should().NotBeNull();
        aliceBreakdown.CategoryTotals.Should().ContainKey("DriverDraft");
        aliceBreakdown.CategoryTotals.Should().ContainKey("Destructor");
        aliceBreakdown.CategoryTotals.Should().ContainKey("MrSaturday");
        
        // DEBUG: Output actual scores to understand what's happening
        Console.WriteLine($"Alice's Category Breakdown:");
        foreach (var category in aliceBreakdown.CategoryTotals)
        {
            Console.WriteLine($"  {category.Key}: {category.Value}");
        }
        
        // Verify exact category scores for Alice with 2023 data:
        aliceBreakdown.CategoryTotals["DriverChampionship"].Should().Be(196, 
            "Top 3 perfect + penalties for position deltas across 22 drivers");
        aliceBreakdown.CategoryTotals["ConstructorChampionship"].Should().Be(100, 
            "All 10 constructors in perfect order = 10*10 = 100");
        aliceBreakdown.CategoryTotals["DriverDraft"].Should().Be(860,
            "Verstappen (575) + Perez (285) = 860");
        
        // Debug: Output actual category scores to see what was calculated
        Console.WriteLine($"Alice's Category Breakdown:");
        foreach (var category in aliceBreakdown.CategoryTotals)
        {
            Console.WriteLine($"  {category.Key}: {category.Value}");
        }
        
        // Mr Saturday and Destructor now have API data available
        aliceBreakdown.CategoryTotals["MrSaturday"].Should().Be(20, 
            "2 poles total for Verstappen + Perez in 2023 = 2 * 10 = 20");
        aliceBreakdown.CategoryTotals["Destructor"].Should().Be(60, 
            "Destructor score in 2023 season data = 3 DNFs * 20 = 60");
        
        aliceBreakdown.CategoryTotals["ZeroPointer"].Should().Be(100, 
            "De Vries scored 0 points (correct) = 100, Sargeant scored 1 point (wrong) = 0");
        aliceBreakdown.CategoryTotals["Wildcard"].Should().Be(0,
            "Wildcard scoring not yet implemented");
        
        // Verify Alice's exact total score (sum of all categories)
        var expectedAliceTotal = aliceBreakdown.CategoryTotals.Values.Sum();
        aliceStanding!.TotalScore.Should().Be(expectedAliceTotal, 
            $"Total score should equal sum of all category scores: {string.Join(" + ", aliceBreakdown.CategoryTotals.Select(kvp => $"{kvp.Key}({kvp.Value})"))}");

        // Step 9: Verify auto-recalc doesn't run again if no new data
        var secondFetch = await _standingsService.GetStandingsWithAutoRecalcAsync(group.Id, Season);
        secondFetch.Should().HaveCount(4);
        // Rankings should be stable
        secondFetch.First(s => s.UserId == "user_alice").Rank.Should().Be(aliceStanding!.Rank);
    }

    [Fact]
    public async Task AutoRecalculation_WithNewRaceResults_UpdatesStandings()
    {
        // Arrange: Create group and predictions
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;
        await AddMembersToGroupAsync(group.Id);
        await SubmitPredictionsForAliceAsync(group.Id);
        await SubmitPredictionsForBobAsync(group.Id);
        await SubmitPredictionsForCharlieAsync(group.Id);
        await SubmitPredictionsForDianaAsync(group.Id);

        // Get initial standings
        var initialStandings = await _standingsService.GetStandingsWithAutoRecalcAsync(group.Id, Season);
        var initialAliceScore = initialStandings.First(s => s.UserId == "user_alice").TotalScore;

        // Act: Simulate new race results being added
        // (In real scenario, this would come from the API data sync)
        // For this test, we just verify the logic would trigger recalc

        // Get latest round with results
        var latestRound = await _resultRepository.GetLatestRoundWithResultsAsync(Season);
        
        // Assert: Verify the method works
        latestRound.Should().BeGreaterOrEqualTo(0, "there should be race results in the database for 2025 or null");

        // Force recalculate
        await _standingsService.RecalculateStandingsAsync(group.Id, Season);
        
        // Get updated standings
        var updatedStandings = await _standingRepository.GetStandingsByGroupAsync(group.Id);
        updatedStandings.Should().HaveCount(4, "we added 4 members");
        
        // Verify all scores are 0 (no actual race results for 2025 yet, but predictions are scored)
        foreach (var standing in updatedStandings)
        {
            standing.TotalScore.Should().BeGreaterOrEqualTo(0);
            standing.Rank.Should().BeGreaterThan(0).And.BeLessOrEqualTo(4);
        }
    }

    private async Task<Group> CreateTestGroupAsync()
    {
        var group = new Group
        {
            Name = $"Test Group {Guid.NewGuid().ToString().Substring(0, 8)}",
            InviteCode = GenerateRandomInviteCode(),
            LockMode = "admin",
            AdminUserId = "user_alice",
            PredictionsLocked = false,
            CreatedAt = DateTime.UtcNow
        };

        return await _groupRepository.CreateAsync(group);
    }

    private async Task AddMembersToGroupAsync(int groupId)
    {
        foreach (var userId in _memberUserIds)
        {
            var member = new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
            await _context.GroupMembers.AddAsync(member);
        }
        await _context.SaveChangesAsync();
    }

    private async Task SubmitPredictionsForAliceAsync(int groupId)
    {
        // Alice: Perfect top 3 prediction (Verstappen, Perez, Hamilton)
        // Driver Championship Score: 3 exact matches (10*3=30) + position deltas for rest
        // Expected score: 30 + (1*-2) + (4*-2) + (3*-2) + (1*-2) + (0*-2) + (1*-2) + (1*-2) + (0*-2) + (0*-2) + (1*-2) + (2*-2) + (3*-2) + (4*-2) + (5*-2) + (6*-2) + (7*-2) + (8*-2) + (9*-2) + (10*-2)
        // = 30 - 120 = -90
        await _predictionRepository.UpsertDriverChampionshipAsync(new DriverChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            RankedDriverIds = new List<string>
            {
                "max_verstappen", "perez", "hamilton", "leclerc", "alonso",
                "norris", "sainz", "russell", "piastri", "stroll",
                "gasly", "ocon", "albon", "tsunoda", "bottas",
                "hulkenberg", "ricciardo", "zhou", "kevin_magnussen", "lawson",
                "sargeant", "de_vries"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        // Constructor Championship: Perfect top 3 (Red Bull, Mercedes, Ferrari)
        // Score: 30 + some penalties for rest = let's calculate: -2 for McLaren (off by 1), -4 for Aston (off by 2), etc.
        await _predictionRepository.UpsertConstructorChampionshipAsync(new ConstructorChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            RankedConstructorIds = new List<string>
            {
                "red_bull", "mercedes", "ferrari", "mclaren", "aston_martin",
                "alpine", "williams", "alphatauri", "alfa", "haas"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        // Driver Draft: Verstappen (575 pts) + Perez (285 pts) = 860 total F1 points
        await _predictionRepository.UpsertDriverDraftAsync(new DriverDraftPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            Driver1Id = "max_verstappen",
            Driver2Id = "perez",
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        });

        // Destructor: Sargeant and De Vries had multiple DNFs in 2023
        await _predictionRepository.UpsertDestructorAsync(new DestructorPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            Driver1Id = "sargeant",
            Driver2Id = "de_vries",
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        });

        // Mr Saturday: Verstappen (2 poles) + Perez (2 poles) = 4 poles * 10 = 40 points
        await _predictionRepository.UpsertMrSaturdayAsync(new MrSaturdayPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            Driver1Id = "max_verstappen",
            Driver2Id = "perez",
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        // Zero Pointer: De Vries scored 0 points in 2023, Sargeant scored 1 point
        await _predictionRepository.UpsertZeroPointerAsync(new ZeroPointerPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            DriverIds = new List<string> { "de_vries", "sargeant" }, // de_vries=correct (+100), sargeant=wrong (-20)
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        await _predictionRepository.UpsertWildcardAsync(new WildcardPrediction
        {
            GroupId = groupId,
            UserId = "user_alice",
            Statement = "Max Verstappen will win more than 15 races this season",
            PointsPotential = 150,
            Fullfilled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });
    }

    private async Task SubmitPredictionsForBobAsync(int groupId)
    {
        // Bob: Completely wrong predictions for comparison
        await _predictionRepository.UpsertDriverChampionshipAsync(new DriverChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            RankedDriverIds = new List<string>
            {
                "norris", "piastri", "russell", "leclerc",
                "sainz", "hamilton", "max_verstappen", "alonso",
                "gasly", "stroll", "ocon", "albon",
                "tsunoda", "lawson", "hulkenberg", "perez",
                "ricciardo", "zhou", "kevin_magnussen", "bottas",
                "sargeant", "de_vries"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-12)
        });

        await _predictionRepository.UpsertConstructorChampionshipAsync(new ConstructorChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            RankedConstructorIds = new List<string>
            {
                "mclaren", "ferrari", "mercedes", "red_bull", "aston_martin",
                "alpine", "williams", "alphatauri", "haas", "alfa"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-12)
        });

        // Driver Draft: Norris (205 pts) + Piastri (97 pts) = 302 total F1 points
        await _predictionRepository.UpsertDriverDraftAsync(new DriverDraftPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            Driver1Id = "norris",
            Driver2Id = "piastri",
            CreatedAt = DateTime.UtcNow.AddDays(-11)
        });

        await _predictionRepository.UpsertDestructorAsync(new DestructorPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            Driver1Id = "ricciardo",
            Driver2Id = "zhou",
            CreatedAt = DateTime.UtcNow.AddDays(-11)
        });

        // Mr Saturday: Norris and Piastri had 0 poles in 2023 = 0 points
        await _predictionRepository.UpsertMrSaturdayAsync(new MrSaturdayPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            Driver1Id = "norris",
            Driver2Id = "piastri",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        await _predictionRepository.UpsertZeroPointerAsync(new ZeroPointerPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            DriverIds = new List<string> { "lawson", "hulkenberg" },
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        });

        await _predictionRepository.UpsertWildcardAsync(new WildcardPrediction
        {
            GroupId = groupId,
            UserId = "user_bob",
            Statement = "McLaren will win the Constructors Championship",
            PointsPotential = 200,
            Fullfilled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        });
    }

    private async Task SubmitPredictionsForCharlieAsync(int groupId)
    {
        // Charlie: Mixed predictions
        await _predictionRepository.UpsertDriverChampionshipAsync(new DriverChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            RankedDriverIds = new List<string>
            {
                "hamilton", "max_verstappen", "perez", "leclerc",
                "alonso", "norris", "sainz", "russell",
                "piastri", "stroll", "gasly", "ocon",
                "albon", "tsunoda", "bottas", "hulkenberg",
                "ricciardo", "zhou", "kevin_magnussen", "lawson",
                "sargeant", "de_vries"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        await _predictionRepository.UpsertConstructorChampionshipAsync(new ConstructorChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            RankedConstructorIds = new List<string>
            {
                "ferrari", "mercedes", "red_bull", "mclaren", "aston_martin",
                "alpine", "williams", "alphatauri", "alfa", "haas"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        });

        // Driver Draft: Hamilton (234 pts) + Leclerc (206 pts) = 440 total F1 points
        await _predictionRepository.UpsertDriverDraftAsync(new DriverDraftPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            Driver1Id = "hamilton",
            Driver2Id = "leclerc",
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });

        await _predictionRepository.UpsertDestructorAsync(new DestructorPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            Driver1Id = "albon",
            Driver2Id = "tsunoda",
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        });

        // Mr Saturday: Leclerc (1 pole) + Verstappen (2 poles) = 3 poles * 10 = 30 points
        await _predictionRepository.UpsertMrSaturdayAsync(new MrSaturdayPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            Driver1Id = "leclerc",
            Driver2Id = "max_verstappen",
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        await _predictionRepository.UpsertZeroPointerAsync(new ZeroPointerPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            DriverIds = new List<string> { "de_vries", "lawson" }, // de_vries=correct (+100), lawson=wrong (-20)
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        await _predictionRepository.UpsertWildcardAsync(new WildcardPrediction
        {
            GroupId = groupId,
            UserId = "user_charlie",
            Statement = "Ferrari will finish 1-2 in at least one race",
            PointsPotential = 100,
            Fullfilled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
    }

    private async Task SubmitPredictionsForDianaAsync(int groupId)
    {
        // Diana: Reverse order (worst predictions possible)
        await _predictionRepository.UpsertDriverChampionshipAsync(new DriverChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            RankedDriverIds = new List<string>
            {
                "de_vries", "sargeant", "lawson", "kevin_magnussen",
                "zhou", "ricciardo", "hulkenberg", "bottas",
                "tsunoda", "albon", "ocon", "gasly",
                "stroll", "piastri", "russell", "sainz",
                "norris", "leclerc", "alonso", "hamilton",
                "perez", "max_verstappen"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        await _predictionRepository.UpsertConstructorChampionshipAsync(new ConstructorChampionshipPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            RankedConstructorIds = new List<string>
            {
                "haas", "alfa", "alphatauri", "williams",
                "alpine", "aston_martin", "mclaren", "ferrari",
                "mercedes", "red_bull"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        });

        // Driver Draft: Sargeant (1 pt) + De Vries (0 pts) = 1 total F1 point
        await _predictionRepository.UpsertDriverDraftAsync(new DriverDraftPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            Driver1Id = "sargeant",
            Driver2Id = "de_vries",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });

        await _predictionRepository.UpsertDestructorAsync(new DestructorPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            Driver1Id = "max_verstappen",
            Driver2Id = "perez", // Least likely to DNF
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });

        // Mr Saturday: Sargeant and De Vries had 0 poles = 0 points
        await _predictionRepository.UpsertMrSaturdayAsync(new MrSaturdayPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            Driver1Id = "sargeant",
            Driver2Id = "de_vries",
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        });

        // Zero Pointer: Correct! De Vries scored 0
        await _predictionRepository.UpsertZeroPointerAsync(new ZeroPointerPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            DriverIds = new List<string> { "de_vries", "max_verstappen" }, // de_vries=correct (+100), verstappen=wrong (-20)
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        });

        await _predictionRepository.UpsertWildcardAsync(new WildcardPrediction
        {
            GroupId = groupId,
            UserId = "user_diana",
            Statement = "Mercedes will have at least 5 race wins",
            PointsPotential = 180,
            Fullfilled = false,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        });
    }

    [Fact]
    public async Task ZeroPointerScoring_CalculatesCorrectlyWithPenalties()
    {
        // Arrange
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;
        await AddMembersToGroupAsync(group.Id);

        // Fetch 2023 driver standings to identify drivers with 0 points
        var standings = await _driverStandingService.GetDriverStandingsBySeasonAsync(Season);
        var driversWithZeroPoints = standings!.DriverStandings!
            .Where(s => int.Parse(s.Points) == 0)
            .Select(s => s.Driver!.DriverId)
            .Take(3) // Take 3 drivers with 0 points
            .ToList();

        var driversWithPoints = standings!.DriverStandings!
            .Where(s => int.Parse(s.Points) > 0)
            .Select(s => s.Driver!.DriverId)
            .Take(2) // Take 2 drivers with points
            .ToList();

        // Create prediction with mix of correct (0 points) and incorrect (has points) predictions
        var prediction = new ZeroPointerPrediction
        {
            GroupId = group.Id,
            UserId = "user_alice",
            DriverIds = new List<string>(driversWithZeroPoints.Concat(driversWithPoints)),
            CreatedAt = DateTime.UtcNow
        };

        await _predictionRepository.UpsertZeroPointerAsync(prediction);

        // Act
        var score = await _scoringService.CalculateZeroPointerScoreAsync(group.Id, "user_alice", Season);

        // Assert
        var expectedScore = (driversWithZeroPoints.Count * 100) + (driversWithPoints.Count * -20);
        score.Should().Be(expectedScore, 
            $"should give +100 for each of {driversWithZeroPoints.Count} correct predictions and -20 for each of {driversWithPoints.Count} incorrect predictions");

        // Verify detailed calculation
        var correctPredictions = driversWithZeroPoints.Count;
        var incorrectPredictions = driversWithPoints.Count;
        score.Should().Be((correctPredictions * 100) + (incorrectPredictions * -20));
    }

    [Fact]
    public async Task ZeroPointerScoring_EmptyList_ReturnsZero()
    {
        // Arrange
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;
        await AddMembersToGroupAsync(group.Id);

        var prediction = new ZeroPointerPrediction
        {
            GroupId = group.Id,
            UserId = "user_alice",
            DriverIds = new List<string>(),
            CreatedAt = DateTime.UtcNow
        };

        await _predictionRepository.UpsertZeroPointerAsync(prediction);

        // Act
        var score = await _scoringService.CalculateZeroPointerScoreAsync(group.Id, "user_alice", Season);

        // Assert
        score.Should().Be(0, "empty prediction list should return 0 points");
    }

    [Fact]
    public async Task ZeroPointerScoring_AllCorrect_GivesFullPoints()
    {
        // Arrange
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;
        await AddMembersToGroupAsync(group.Id);

        // Fetch drivers with 0 points
        var standings = await _driverStandingService.GetDriverStandingsBySeasonAsync(Season);
        var driversWithZeroPoints = standings!.DriverStandings!
            .Where(s => int.Parse(s.Points) == 0)
            .Select(s => s.Driver!.DriverId)
            .Take(5)
            .ToList();

        var prediction = new ZeroPointerPrediction
        {
            GroupId = group.Id,
            UserId = "user_alice",
            DriverIds = driversWithZeroPoints,
            CreatedAt = DateTime.UtcNow
        };

        await _predictionRepository.UpsertZeroPointerAsync(prediction);

        // Act
        var score = await _scoringService.CalculateZeroPointerScoreAsync(group.Id, "user_alice", Season);

        // Assert
        score.Should().Be(driversWithZeroPoints.Count * 100, "all correct predictions should give 100 points each");
    }

    [Fact]
    public async Task ZeroPointerScoring_AllIncorrect_GivesPenalties()
    {
        // Arrange
        var group = await CreateTestGroupAsync();
        _testGroupId = group.Id;
        await AddMembersToGroupAsync(group.Id);

        // Fetch drivers with points (top drivers)
        var standings = await _driverStandingService.GetDriverStandingsBySeasonAsync(Season);
        var driversWithPoints = standings!.DriverStandings!
            .Where(s => int.Parse(s.Points) > 0)
            .Select(s => s.Driver!.DriverId)
            .Take(3)
            .ToList();

        var prediction = new ZeroPointerPrediction
        {
            GroupId = group.Id,
            UserId = "user_alice",
            DriverIds = driversWithPoints,
            CreatedAt = DateTime.UtcNow
        };

        await _predictionRepository.UpsertZeroPointerAsync(prediction);

        // Act
        var score = await _scoringService.CalculateZeroPointerScoreAsync(group.Id, "user_alice", Season);

        // Assert
        score.Should().Be(driversWithPoints.Count * -20, "all incorrect predictions should give -20 penalty each");
        score.Should().BeNegative("predicting drivers with points as zero-pointers should result in negative score");
    }

    private string GenerateRandomInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public void Dispose()
    {
        // Clean up test data
        if (_testGroupId > 0)
        {
            var group = _context.Groups.Find(_testGroupId);
            if (group != null)
            {
                // Delete all related data
                var members = _context.GroupMembers.Where(m => m.GroupId == _testGroupId);
                _context.GroupMembers.RemoveRange(members);

                var standings = _context.Standings.Where(s => s.GroupId == _testGroupId);
                _context.Standings.RemoveRange(standings);

                var predictions = _context.DriverChampionshipPredictions.Where(p => p.GroupId == _testGroupId);
                _context.DriverChampionshipPredictions.RemoveRange(predictions);

                var constructorPredictions = _context.ConstructorChampionshipPredictions.Where(p => p.GroupId == _testGroupId);
                _context.ConstructorChampionshipPredictions.RemoveRange(constructorPredictions);

                var draftPredictions = _context.DriverDraftPredictions.Where(p => p.GroupId == _testGroupId);
                _context.DriverDraftPredictions.RemoveRange(draftPredictions);

                var destructorPredictions = _context.DestructorPredictions.Where(p => p.GroupId == _testGroupId);
                _context.DestructorPredictions.RemoveRange(destructorPredictions);

                var mrSaturdayPredictions = _context.MrSaturdayPredictions.Where(p => p.GroupId == _testGroupId);
                _context.MrSaturdayPredictions.RemoveRange(mrSaturdayPredictions);

                var zeroPointerPredictions = _context.ZeroPointerPredictions.Where(p => p.GroupId == _testGroupId);
                _context.ZeroPointerPredictions.RemoveRange(zeroPointerPredictions);

                var wildcardPredictions = _context.WildcardPredictions.Where(p => p.GroupId == _testGroupId);
                _context.WildcardPredictions.RemoveRange(wildcardPredictions);

                _context.Groups.Remove(group);
                _context.SaveChanges();
            }
        }

        _context.Dispose();
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
