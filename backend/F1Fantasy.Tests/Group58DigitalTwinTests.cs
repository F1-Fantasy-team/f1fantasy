using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace F1Fantasy.Tests;

/// <summary>
/// Digital Twin test for Group 58
/// Clones group 58's predictions and simulates Round 1 (Australia) results to verify standings calculation
/// </summary>
[Collection("Sequential")]
public class Group58DigitalTwinTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly GroupRepository _groupRepository;
    private readonly PredictionRepository _predictionRepository;
    private readonly StandingRepository _standingRepository;
    private readonly ResultRepository _resultRepository;
    private readonly RaceRepository _raceRepository;
    private readonly QualifyingRepository _qualifyingRepository;
    private readonly DriverStandingRepository _driverStandingRepository;
    private readonly ConstructorStandingRepository _constructorStandingRepository;
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly HttpClient _httpClient;
    private readonly ResultService _resultService;
    private readonly RaceService _raceService;
    private readonly QualifyingService _qualifyingService;
    private readonly DriverStandingService _driverStandingService;
    private readonly ConstructorStandingService _constructorStandingService;
    private readonly ScoringService _scoringService;
    private readonly StandingsService _standingsService;
    
    private const int SourceGroupId = 58; // The real group to clone
    private const int TwinGroupId = 99958; // Digital twin group
    private const string Season = "2026";

    public Group58DigitalTwinTests()
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
        
        // Use separate DbContext instances to avoid concurrency issues
        var contextFactory = new TestDbContextFactory(options);
        
        _context = new F1FantasyDbContext(options);
        var groupContext = new F1FantasyDbContext(options);
        var standingContext = new F1FantasyDbContext(options);
        var resultContext = new F1FantasyDbContext(options);
        var raceContext = new F1FantasyDbContext(options);
        var qualifyingContext = new F1FantasyDbContext(options);
        var driverStandingContext = new F1FantasyDbContext(options);
        var constructorStandingContext = new F1FantasyDbContext(options);
        var metadataContext = new F1FantasyDbContext(options);
        
        // Initialize repositories with separate contexts
        _groupRepository = new GroupRepository(groupContext, NullLogger<GroupRepository>.Instance);
        _predictionRepository = new PredictionRepository(contextFactory);
        _standingRepository = new StandingRepository(standingContext);
        _resultRepository = new ResultRepository(resultContext, NullLogger<ResultRepository>.Instance);
        _raceRepository = new RaceRepository(raceContext, NullLogger<RaceRepository>.Instance);
        _qualifyingRepository = new QualifyingRepository(qualifyingContext, NullLogger<QualifyingRepository>.Instance);
        _driverStandingRepository = new DriverStandingRepository(driverStandingContext, NullLogger<DriverStandingRepository>.Instance);
        _constructorStandingRepository = new ConstructorStandingRepository(constructorStandingContext, NullLogger<ConstructorStandingRepository>.Instance);
        _metadataRepository = new DataFetchMetadataRepository(metadataContext, NullLogger<DataFetchMetadataRepository>.Instance);
        
        // Initialize HTTP client and F1 data services
        _httpClient = new HttpClient();
        var cacheStalenessService = new CacheStalenessService(_metadataRepository, _raceRepository, NullLogger<CacheStalenessService>.Instance);
        
        _resultService = new ResultService(_httpClient, _resultRepository, _metadataRepository, cacheStalenessService, NullLogger<ResultService>.Instance);
        _raceService = new RaceService(_httpClient, _raceRepository, _metadataRepository, cacheStalenessService, NullLogger<RaceService>.Instance);
        
        _qualifyingService = new QualifyingService(_httpClient, _qualifyingRepository, _metadataRepository, cacheStalenessService, NullLogger<QualifyingService>.Instance);
        _driverStandingService = new DriverStandingService(_httpClient, _driverStandingRepository, _metadataRepository, cacheStalenessService, NullLogger<DriverStandingService>.Instance);
        _constructorStandingService = new ConstructorStandingService(_httpClient, _constructorStandingRepository, _metadataRepository, cacheStalenessService, NullLogger<ConstructorStandingService>.Instance);
        
        // Initialize services
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
    public async Task DigitalTwin_CloneGroup58AndSimulateAustraliaRound1_CalculatesStandings()
    {
        // Arrange - Clean up any previous test data
        await CleanupDigitalTwinAsync();
        
        // Step 1: Verify source group exists
        var sourceGroup = await _groupRepository.GetByIdAsync(SourceGroupId);
        sourceGroup.Should().NotBeNull($"Source group {SourceGroupId} must exist");
        
        var sourceMembers = await _groupRepository.GetMembersAsync(SourceGroupId);
        sourceMembers.Should().NotBeEmpty($"Source group {SourceGroupId} must have members");
        
        Console.WriteLine($"\n=== DIGITAL TWIN TEST ===");
        Console.WriteLine($"Source Group: {sourceGroup!.Name} (ID: {SourceGroupId})");
        Console.WriteLine($"Members: {sourceMembers.Count}");
        foreach (var member in sourceMembers)
        {
            Console.WriteLine($"  - {member.UserId}");
        }
        
        // Step 2: Create digital twin group
        var twinGroup = await CreateDigitalTwinGroupAsync(sourceGroup);
        Console.WriteLine($"\nCreated Digital Twin: {twinGroup.Name} (ID: {TwinGroupId})");
        
        // Step 3: Clone all members
        var memberMapping = new Dictionary<string, string>(); // Original userId -> Twin userId
        var twinMembers = new List<GroupMember>();
        
        foreach (var sourceMember in sourceMembers)
        {
            var twinUserId = $"twin_{sourceMember.UserId}";
            memberMapping[sourceMember.UserId] = twinUserId;
            
            var twinMember = await _groupRepository.AddMemberAsync(new GroupMember
            {
                GroupId = TwinGroupId,
                UserId = twinUserId,
                JoinedAt = sourceMember.JoinedAt
            });
            twinMembers.Add(twinMember);
        }
        Console.WriteLine($"Cloned {twinMembers.Count} members");
        
        // Step 4: Clone all predictions for each member
        await ClonePredictionsAsync(sourceMembers, memberMapping);
        
        // Step 5: Insert mock Round 1 (Australia) race results
        await InsertMockAustraliaRound1ResultsAsync();
        
        // Step 6: Insert mock Round 1 qualifying results
        await InsertMockAustraliaRound1QualifyingAsync();
        
        // Step 7: Insert mock driver standings after Round 1
        await InsertMockDriverStandingsRound1Async();
        
        // Step 8: Insert mock constructor standings after Round 1
        await InsertMockConstructorStandingsRound1Async();
        
        // Step 9: Calculate standings for digital twin group
        // Note: We calculate each category sequentially (not using CalculateAllCategoryScoresAsync which parallelizes)
        // to avoid DbContext concurrency issues in tests
        Console.WriteLine($"\n=== CALCULATING STANDINGS ===");
        
        var standingsToSave = new List<Standing>();
        foreach (var twinMember in twinMembers)
        {
            // Calculate each category separately with fresh contexts to avoid concurrency
            var categoryScores = new Dictionary<string, int>();
            
            // Constructor Championship
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var metadata = new DataFetchMetadataRepository(ctx, NullLogger<DataFetchMetadataRepository>.Instance);
                var raceRepo = new RaceRepository(ctx, NullLogger<RaceRepository>.Instance);
                var cacheStaleness = new CacheStalenessService(metadata, raceRepo, NullLogger<CacheStalenessService>.Instance);
                var svc = new ConstructorStandingService(_httpClient, 
                    new ConstructorStandingRepository(ctx, NullLogger<ConstructorStandingRepository>.Instance), 
                    metadata,
                    cacheStaleness,
                    NullLogger<ConstructorStandingService>.Instance);
                var scoring = new ScoringService(_predictionRepository, _driverStandingService, svc, _resultService, _qualifyingService, _raceService);
                categoryScores["constructorChampionship"] = await scoring.CalculateConstructorChampionshipScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Driver Championship  
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var metadata = new DataFetchMetadataRepository(ctx, NullLogger<DataFetchMetadataRepository>.Instance);
                var race = new RaceRepository(ctx, NullLogger<RaceRepository>.Instance);
                var cacheStaleness = new CacheStalenessService(metadata, race, NullLogger<CacheStalenessService>.Instance);
                var svc = new DriverStandingService(_httpClient,
                    new DriverStandingRepository(ctx, NullLogger<DriverStandingRepository>.Instance),
                    metadata,
                    cacheStaleness,
                    NullLogger<DriverStandingService>.Instance);
                var scoring = new ScoringService(_predictionRepository, svc, _constructorStandingService, _resultService, _qualifyingService, _raceService);
                categoryScores["driverChampionship"] = await scoring.CalculateDriverChampionshipScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Driver Draft
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var metadata = new DataFetchMetadataRepository(ctx, NullLogger<DataFetchMetadataRepository>.Instance);
                var race = new RaceRepository(ctx, NullLogger<RaceRepository>.Instance);
                var cacheStaleness = new CacheStalenessService(metadata, race, NullLogger<CacheStalenessService>.Instance);
                var svc = new DriverStandingService(_httpClient,
                    new DriverStandingRepository(ctx, NullLogger<DriverStandingRepository>.Instance),
                    metadata,
                    cacheStaleness,
                    NullLogger<DriverStandingService>.Instance);
                var scoring = new ScoringService(_predictionRepository, svc, _constructorStandingService, _resultService, _qualifyingService, _raceService);
                categoryScores["driverDraft"] = await scoring.CalculateDriverDraftScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Destructor
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var cacheStaleness = new CacheStalenessService(_metadataRepository, _raceRepository, NullLogger<CacheStalenessService>.Instance);
                var svc = new ResultService(_httpClient,
                    new ResultRepository(ctx, NullLogger<ResultRepository>.Instance),
                    _metadataRepository, cacheStaleness,
                    NullLogger<ResultService>.Instance);
                var scoring = new ScoringService(_predictionRepository, _driverStandingService, _constructorStandingService, svc, _qualifyingService, _raceService);
                categoryScores["destructor"] = await scoring.CalculateDestructorScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Mr Saturday
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var metadata = new DataFetchMetadataRepository(ctx, NullLogger<DataFetchMetadataRepository>.Instance);
                var raceRepo = new RaceRepository(ctx, NullLogger<RaceRepository>.Instance);
                var cacheStaleness = new CacheStalenessService(metadata, raceRepo, NullLogger<CacheStalenessService>.Instance);
                var svc = new QualifyingService(_httpClient,
                    new QualifyingRepository(ctx, NullLogger<QualifyingRepository>.Instance),
                    metadata,
                    cacheStaleness,
                    NullLogger<QualifyingService>.Instance);
                var scoring = new ScoringService(_predictionRepository, _driverStandingService, _constructorStandingService, _resultService, svc, _raceService);
                categoryScores["mrSaturday"] = await scoring.CalculateMrSaturdayScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Zero Pointer
            using (var ctx = new F1FantasyDbContext(new DbContextOptionsBuilder<F1FantasyDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")).Options))
            {
                var metadata = new DataFetchMetadataRepository(ctx, NullLogger<DataFetchMetadataRepository>.Instance);
                var race = new RaceRepository(ctx, NullLogger<RaceRepository>.Instance);
                var cacheStaleness = new CacheStalenessService(metadata, race, NullLogger<CacheStalenessService>.Instance);
                var svc = new DriverStandingService(_httpClient,
                    new DriverStandingRepository(ctx, NullLogger<DriverStandingRepository>.Instance),
                    metadata,
                    cacheStaleness,
                    NullLogger<DriverStandingService>.Instance);
                var scoring = new ScoringService(_predictionRepository, svc, _constructorStandingService, _resultService, _qualifyingService, _raceService);
                categoryScores["zeroPointer"] = await scoring.CalculateZeroPointerScoreAsync(TwinGroupId, twinMember.UserId, Season);
            }
            
            // Wildcard
            categoryScores["wildcard"] = await _scoringService.CalculateWildcardScoreAsync(TwinGroupId, twinMember.UserId);
            
            var totalScore = categoryScores.Values.Sum();
            
            standingsToSave.Add(new Standing
            {
                GroupId = TwinGroupId,
                UserId = twinMember.UserId,
                TotalScore = totalScore,
                CategoryScoresJson = System.Text.Json.JsonSerializer.Serialize(categoryScores),
                Rank = 0, // Will be set after sorting
                UpdatedAt = DateTime.UtcNow
            });
        }
        
        // Sort and assign ranks
        var rankedStandings = standingsToSave
            .OrderByDescending(s => s.TotalScore)
            .ToList();
        
        for (int i = 0; i < rankedStandings.Count; i++)
        {
            rankedStandings[i].Rank = i + 1;
            await _standingRepository.UpsertAsync(rankedStandings[i]);
        }
        
        var standings = await _standingRepository.GetStandingsByGroupAsync(TwinGroupId);
        standings.Should().HaveCount(twinMembers.Count);
        
        Console.WriteLine($"\n=== STANDINGS RESULTS ===");
        foreach (var standing in standings.OrderBy(s => s.Rank))
        {
            var originalUserId = memberMapping.First(kvp => kvp.Value == standing.UserId).Key;
            Console.WriteLine($"Rank {standing.Rank}: {originalUserId} - {standing.TotalScore} points");
        }
        
        // Step 10: Display detailed breakdown from the scores we already calculated
        Console.WriteLine($"\n=== DETAILED BREAKDOWNS ===");
        foreach (var standing in rankedStandings)
        {
            var originalUserId = memberMapping.First(kvp => kvp.Value == standing.UserId).Key;
            var categoryScores = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(standing.CategoryScoresJson ?? "{}");
            
            Console.WriteLine($"\n{originalUserId}:");
            Console.WriteLine($"  Total Score: {standing.TotalScore}");
            Console.WriteLine($"  Categories:");
            foreach (var category in categoryScores!.OrderByDescending(c => c.Value))
            {
                Console.WriteLine($"    {category.Key}: {category.Value}");
            }
        }
        
        // Verify Zero Pointer behavior
        Console.WriteLine($"\n=== ZERO POINTER VERIFICATION ===");
        foreach (var standing in rankedStandings)
        {
            var originalUserId = memberMapping.First(kvp => kvp.Value == standing.UserId).Key;
            var categoryScores = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(standing.CategoryScoresJson ?? "{}");
            var zeroPointerScore = categoryScores!.GetValueOrDefault("zeroPointer", 0);
            Console.WriteLine($"{originalUserId}: Zero Pointer = {zeroPointerScore} (expected 0 because season incomplete)");
            zeroPointerScore.Should().Be(0, $"Zero Pointer should be 0 for {originalUserId} because season is incomplete (only Round 1 of ~24 races)");
        }
        
        // Assertions
        standings.Should().AllSatisfy(s => s.Rank.Should().BeGreaterThan(0));
        standings.Select(s => s.Rank).Distinct().Should().HaveCount(standings.Count, "all ranks should be unique");
    }

    private async Task<Group> CreateDigitalTwinGroupAsync(Group sourceGroup)
    {
        // Delete if exists
        var existing = await _context.Groups.FindAsync(TwinGroupId);
        if (existing != null)
        {
            await _groupRepository.DeleteAsync(TwinGroupId);
        }
        
        var twinGroup = new Group
        {
            Id = TwinGroupId,
            Name = $"Digital Twin of {sourceGroup.Name}",
            AdminUserId = $"twin_{sourceGroup.AdminUserId}",
            InviteCode = "TWIN9998",
            LockMode = sourceGroup.LockMode,
            CreatedAt = DateTime.UtcNow,
            PredictionsLocked = false
        };
        
        _context.Groups.Add(twinGroup);
        await _context.SaveChangesAsync();
        return twinGroup;
    }

    private async Task ClonePredictionsAsync(List<GroupMember> sourceMembers, Dictionary<string, string> memberMapping)
    {
        Console.WriteLine($"\n=== CLONING PREDICTIONS ===");
        int totalPredictions = 0;
        
        foreach (var sourceMember in sourceMembers)
        {
            var twinUserId = memberMapping[sourceMember.UserId];
            var predictions = await _predictionRepository.GetAllPredictionsAsync(SourceGroupId, sourceMember.UserId);
            
            // Clone Constructor Championship
            if (predictions.ConstructorChampionship != null)
            {
                await _predictionRepository.UpsertConstructorChampionshipAsync(new ConstructorChampionshipPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    RankedConstructorIds = predictions.ConstructorChampionship.RankedConstructorIds
                });
                totalPredictions++;
            }
            
            // Clone Driver Championship
            if (predictions.DriverChampionship != null)
            {
                await _predictionRepository.UpsertDriverChampionshipAsync(new DriverChampionshipPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    RankedDriverIds = predictions.DriverChampionship.RankedDriverIds
                });
                totalPredictions++;
            }
            
            // Clone Driver Draft
            if (predictions.DriverDraft != null)
            {
                await _predictionRepository.UpsertDriverDraftAsync(new DriverDraftPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    Driver1Id = predictions.DriverDraft.Driver1Id,
                    Driver2Id = predictions.DriverDraft.Driver2Id
                });
                totalPredictions++;
            }
            
            // Clone Destructor
            if (predictions.Destructor != null)
            {
                await _predictionRepository.UpsertDestructorAsync(new DestructorPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    Driver1Id = predictions.Destructor.Driver1Id,
                    Driver2Id = predictions.Destructor.Driver2Id
                });
                totalPredictions++;
            }
            
            // Clone Mr Saturday
            if (predictions.MrSaturday != null)
            {
                await _predictionRepository.UpsertMrSaturdayAsync(new MrSaturdayPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    Driver1Id = predictions.MrSaturday.Driver1Id,
                    Driver2Id = predictions.MrSaturday.Driver2Id
                });
                totalPredictions++;
            }
            
            // Clone Zero Pointer
            if (predictions.ZeroPointer != null)
            {
                await _predictionRepository.UpsertZeroPointerAsync(new ZeroPointerPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    DriverIds = predictions.ZeroPointer.DriverIds
                });
                totalPredictions++;
            }
            
            // Clone Wildcard
            if (predictions.Wildcard != null)
            {
                await _predictionRepository.UpsertWildcardAsync(new WildcardPrediction
                {
                    GroupId = TwinGroupId,
                    UserId = twinUserId,
                    Statement = predictions.Wildcard.Statement,
                    PointsPotential = predictions.Wildcard.PointsPotential,
                    Fullfilled = predictions.Wildcard.Fullfilled
                });
                totalPredictions++;
            }
        }
        
        Console.WriteLine($"Cloned {totalPredictions} predictions across {sourceMembers.Count} members");
    }

    private async Task InsertMockAustraliaRound1ResultsAsync()
    {
        Console.WriteLine($"\n=== INSERTING MOCK RACE RESULTS (Australia Round 1) ===");
        
        // Mock race results for Australia 2026 Round 1
        var mockResults = new[]
        {
            new { Position = 1, DriverId = "norris", ConstructorId = "mclaren", Points = 25, Grid = 1, Status = "Finished" },
            new { Position = 2, DriverId = "leclerc", ConstructorId = "ferrari", Points = 18, Grid = 3, Status = "Finished" },
            new { Position = 3, DriverId = "piastri", ConstructorId = "mclaren", Points = 15, Grid = 2, Status = "Finished" },
            new { Position = 4, DriverId = "sainz", ConstructorId = "ferrari", Points = 12, Grid = 4, Status = "Finished" },
            new { Position = 5, DriverId = "russell", ConstructorId = "mercedes", Points = 10, Grid = 5, Status = "Finished" },
            new { Position = 6, DriverId = "hamilton", ConstructorId = "mercedes", Points = 8, Grid = 7, Status = "Finished" },
            new { Position = 7, DriverId = "verstappen", ConstructorId = "red_bull", Points = 6, Grid = 6, Status = "Finished" },
            new { Position = 8, DriverId = "alonso", ConstructorId = "aston_martin", Points = 4, Grid = 8, Status = "Finished" },
            new { Position = 9, DriverId = "gasly", ConstructorId = "alpine", Points = 2, Grid = 10, Status = "Finished" },
            new { Position = 10, DriverId = "hulkenberg", ConstructorId = "haas", Points = 1, Grid = 12, Status = "Finished" },
            new { Position = 11, DriverId = "stroll", ConstructorId = "aston_martin", Points = 0, Grid = 9, Status = "Finished" },
            new { Position = 12, DriverId = "ocon", ConstructorId = "alpine", Points = 0, Grid = 11, Status = "Finished" },
            new { Position = 13, DriverId = "tsunoda", ConstructorId = "rb", Points = 0, Grid = 14, Status = "Finished" },
            new { Position = 14, DriverId = "albon", ConstructorId = "williams", Points = 0, Grid = 13, Status = "Finished" },
            new { Position = 15, DriverId = "bearman", ConstructorId = "haas", Points = 0, Grid = 15, Status = "Finished" },
            new { Position = 16, DriverId = "colapinto", ConstructorId = "williams", Points = 0, Grid = 16, Status = "Finished" },
            new { Position = 17, DriverId = "lawson", ConstructorId = "rb", Points = 0, Grid = 18, Status = "+1 Lap" },
            new { Position = 18, DriverId = "bottas", ConstructorId = "sauber", Points = 0, Grid = 17, Status = "+2 Laps" },
            new { Position = 19, DriverId = "perez", ConstructorId = "red_bull", Points = 0, Grid = 19, Status = "Engine" }, // DNF
            new { Position = 20, DriverId = "zhou", ConstructorId = "sauber", Points = 0, Grid = 20, Status = "Collision" }, // DNF
        };
        
        foreach (var result in mockResults)
        {
            await _resultRepository.AddOrUpdateAsync(new Result
            {
                Season = Season,
                Round = "1",
                Number = result.Position.ToString(),
                Position = result.Position.ToString(),
                PositionText = result.Position.ToString(),
                Points = result.Points.ToString(),
                DriverId = result.DriverId,
                Driver = new Driver { DriverId = result.DriverId },
                ConstructorId = result.ConstructorId,
                Constructor = new Constructor { ConstructorId = result.ConstructorId },
                Grid = result.Grid.ToString(),
                Laps = result.Status == "Finished" ? "58" : (result.Position - 10).ToString(),
                Status = result.Status,
                IsSprint = false
            }, Season, "1");
        }
        
        Console.WriteLine($"Inserted {mockResults.Length} race results for Round 1");
    }

    private async Task InsertMockAustraliaRound1QualifyingAsync()
    {
        Console.WriteLine($"\n=== INSERTING MOCK QUALIFYING RESULTS (Australia Round 1) ===");
        
        // Mock qualifying results - Norris on pole
        var mockQualifying = new[]
        {
            new { Position = 1, DriverId = "norris", ConstructorId = "mclaren", Q3 = "1:15.123" },
            new { Position = 2, DriverId = "piastri", ConstructorId = "mclaren", Q3 = "1:15.234" },
            new { Position = 3, DriverId = "leclerc", ConstructorId = "ferrari", Q3 = "1:15.345" },
            new { Position = 4, DriverId = "sainz", ConstructorId = "ferrari", Q3 = "1:15.456" },
            new { Position = 5, DriverId = "russell", ConstructorId = "mercedes", Q3 = "1:15.567" },
            new { Position = 6, DriverId = "verstappen", ConstructorId = "red_bull", Q3 = "1:15.678" },
            new { Position = 7, DriverId = "hamilton", ConstructorId = "mercedes", Q3 = "1:15.789" },
            new { Position = 8, DriverId = "alonso", ConstructorId = "aston_martin", Q3 = "1:15.890" },
            new { Position = 9, DriverId = "stroll", ConstructorId = "aston_martin", Q3 = "1:15.901" },
            new { Position = 10, DriverId = "gasly", ConstructorId = "alpine", Q3 = "1:16.012" },
        };
        
        foreach (var q in mockQualifying)
        {
            await _qualifyingRepository.AddOrUpdateAsync(new Qualifying
            {
                Season = Season,
                Round = "1",
                DriverId = q.DriverId,
                ConstructorId = q.ConstructorId,
                Position = q.Position.ToString(),
                Q1 = q.Q3,
                Q2 = q.Q3,
                Q3 = q.Q3
            }, Season, "1");
        }
        
        Console.WriteLine($"Inserted {mockQualifying.Length} qualifying results");
    }

    private async Task InsertMockDriverStandingsRound1Async()
    {
        Console.WriteLine($"\n=== INSERTING MOCK DRIVER STANDINGS (After Round 1) ===");
        
        var mockStandings = new[]
        {
            new { Position = 1, DriverId = "norris", Points = 25, Wins = 1 },
            new { Position = 2, DriverId = "leclerc", Points = 18, Wins = 0 },
            new { Position = 3, DriverId = "piastri", Points = 15, Wins = 0 },
            new { Position = 4, DriverId = "sainz", Points = 12, Wins = 0 },
            new { Position = 5, DriverId = "russell", Points = 10, Wins = 0 },
            new { Position = 6, DriverId = "hamilton", Points = 8, Wins = 0 },
            new { Position = 7, DriverId = "verstappen", Points = 6, Wins = 0 },
            new { Position = 8, DriverId = "alonso", Points = 4, Wins = 0 },
            new { Position = 9, DriverId = "gasly", Points = 2, Wins = 0 },
            new { Position = 10, DriverId = "hulkenberg", Points = 1, Wins = 0 },
        };
        
        foreach (var standing in mockStandings)
        {
            await _driverStandingRepository.AddOrUpdateAsync(new DriverStanding
            {
                Season = Season,
                DriverId = standing.DriverId,
                Round = "1",
                Position = standing.Position.ToString(),
                PositionText = standing.Position.ToString(),
                Points = standing.Points.ToString(),
                Wins = standing.Wins.ToString(),
                ConstructorId = "mclaren" // Simplified
            });
        }
        
        Console.WriteLine($"Inserted {mockStandings.Length} driver standings");
    }

    private async Task InsertMockConstructorStandingsRound1Async()
    {
        Console.WriteLine($"\n=== INSERTING MOCK CONSTRUCTOR STANDINGS (After Round 1) ===");
        
        var mockStandings = new[]
        {
            new { Position = 1, ConstructorId = "mclaren", Points = 40, Wins = 1 }, // Norris 25 + Piastri 15
            new { Position = 2, ConstructorId = "ferrari", Points = 30, Wins = 0 }, // Leclerc 18 + Sainz 12
            new { Position = 3, ConstructorId = "mercedes", Points = 18, Wins = 0 }, // Russell 10 + Hamilton 8
            new { Position = 4, ConstructorId = "red_bull", Points = 6, Wins = 0 }, // Verstappen 6 + Perez DNF
            new { Position = 5, ConstructorId = "aston_martin", Points = 4, Wins = 0 }, // Alonso 4 + Stroll 0
            new { Position = 6, ConstructorId = "alpine", Points = 2, Wins = 0 }, // Gasly 2 + Ocon 0
            new { Position = 7, ConstructorId = "haas", Points = 1, Wins = 0 }, // Hulkenberg 1 + Bearman 0
            new { Position = 8, ConstructorId = "rb", Points = 0, Wins = 0 },
            new { Position = 9, ConstructorId = "williams", Points = 0, Wins = 0 },
            new { Position = 10, ConstructorId = "sauber", Points = 0, Wins = 0 },
        };
        
        foreach (var standing in mockStandings)
        {
            await _constructorStandingRepository.AddOrUpdateAsync(new ConstructorStanding
            {
                Season = Season,
                ConstructorId = standing.ConstructorId,
                Round = "1",
                Position = standing.Position.ToString(),
                PositionText = standing.Position.ToString(),
                Points = standing.Points.ToString(),
                Wins = standing.Wins.ToString()
            });
        }
        
        Console.WriteLine($"Inserted {mockStandings.Length} constructor standings");
    }

    private async Task CleanupDigitalTwinAsync()
    {
        // Delete digital twin group and all associated data
        var existingGroup = await _context.Groups.FindAsync(TwinGroupId);
        if (existingGroup != null)
        {
            await _groupRepository.DeleteAsync(TwinGroupId);
        }
        
        // Delete test results for 2026 Round 1
        var testResults = _context.Results.Where(r => r.Season == Season && r.Round == "1");
        _context.Results.RemoveRange(testResults);
        
        var testQualifying = _context.Qualifyings.Where(q => q.Season == Season && q.Round == "1");
        _context.Qualifyings.RemoveRange(testQualifying);
        
        var testDriverStandings = _context.DriverStandings.Where(d => d.Season == Season);
        _context.DriverStandings.RemoveRange(testDriverStandings);
        
        var testConstructorStandings = _context.ConstructorStandings.Where(c => c.Season == Season);
        _context.ConstructorStandings.RemoveRange(testConstructorStandings);
        
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        CleanupDigitalTwinAsync().Wait();
        _context.Dispose();
        _httpClient.Dispose();
    }
}
