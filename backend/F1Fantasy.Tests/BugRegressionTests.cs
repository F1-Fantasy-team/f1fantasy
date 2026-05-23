using F1Fantasy.Data;
using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using Xunit;

namespace F1Fantasy.Tests;

/// <summary>
/// Regression tests for confirmed bugs. Each test documents the expected (correct) behavior.
/// Tests that exercise the bug path are currently FAILING until the bug is fixed.
/// </summary>
[Collection("Sequential")]
public class BugRegressionTests : IDisposable
{
    private readonly F1FantasyDbContext _context;
    private readonly DbContextOptions<F1FantasyDbContext> _dbOptions;

    public BugRegressionTests()
    {
        _dbOptions = new DbContextOptionsBuilder<F1FantasyDbContext>()
            .UseInMemoryDatabase(databaseName: "BugRegression_" + Guid.NewGuid())
            .Options;
        _context = new F1FantasyDbContext(_dbOptions);
    }

    // ── Bug #3: CacheStalenessService inverted buffer ─────────────────────────
    // CacheStalenessService.cs:85 uses `raceDate < UtcNow.Add(buffer)` which
    // includes FUTURE races in the "data available" window.
    // Correct logic: `raceDate.Add(buffer) < UtcNow` (data available after buffer elapses).

    [Fact]
    public async Task CacheStaleness_FutureRaceWithinBuffer_ShouldNotTriggerRefetch()
    {
        // Arrange: cache freshly populated 30 minutes ago
        const string season = "2099";
        _context.DataFetchMetadata.Add(new DataFetchMetadata
        {
            Season = season,
            DataType = "Results",
            LastFetchedAt = DateTime.UtcNow.AddMinutes(-30),
            FetchSuccessful = true
        });
        // Race happening 23 hours from now — data is NOT yet available
        _context.Races.Add(new Race
        {
            Season = season,
            Round = "1",
            RaceName = "Future GP",
            Date = DateTime.UtcNow.AddHours(23).ToString("yyyy-MM-dd")
        });
        await _context.SaveChangesAsync();

        var sut = BuildCacheStalenessService();

        // Act
        var shouldFetch = await sut.ShouldFetchAsync(season, DataType.Results, CacheStalenessOptions.ForResults);

        // Assert: cache is fresh AND race hasn't happened — no need to refetch
        // BUG: currently returns true because `raceDate < UtcNow.Add(1day)` is satisfied by tomorrow's race
        shouldFetch.Should().BeFalse(
            "cache is fresh and the race hasn't happened yet, so no new data exists");
    }

    [Fact]
    public async Task CacheStaleness_PastRaceAfterBufferElapsed_ShouldTriggerRefetch()
    {
        // Arrange: cache populated 30 minutes ago, but a race finished 2 days ago
        // (data has been available for >1 day but wasn't fetched after the race)
        const string season = "2098";
        _context.DataFetchMetadata.Add(new DataFetchMetadata
        {
            Season = season,
            DataType = "Results",
            LastFetchedAt = DateTime.UtcNow.AddDays(-3), // Last fetch was 3 days ago
            FetchSuccessful = true
        });
        _context.Races.Add(new Race
        {
            Season = season,
            Round = "1",
            RaceName = "Past GP",
            Date = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd") // Race was 2 days ago
        });
        await _context.SaveChangesAsync();

        var sut = BuildCacheStalenessService();

        // Act
        var shouldFetch = await sut.ShouldFetchAsync(season, DataType.Results, CacheStalenessOptions.ForResults);

        // Assert: race happened 2 days ago, buffer (1 day) has elapsed — refetch is needed
        shouldFetch.Should().BeTrue(
            "the race finished 2 days ago and data has been available for more than the buffer period");
    }

    // ── Bug #2: NullReferenceException on standingEntry.Constructor ────────────
    // ConstructorStandingService.cs:92 accesses standingEntry.Constructor.ConstructorId
    // with no null guard — unlike DriverStandingService which skips null entries.

    [Fact]
    public async Task ConstructorStandings_NullConstructorInApiResponse_ShouldSkipEntryAndNotThrow()
    {
        // Arrange: API response where first entry has null Constructor, second is valid
        const string responseJson = """
            {
              "MRData": {
                "StandingsTable": {
                  "StandingsLists": [{
                    "season": "2026",
                    "round": "5",
                    "ConstructorStandings": [
                      {
                        "position": "1",
                        "positionText": "1",
                        "points": "150",
                        "wins": "3",
                        "Constructor": null
                      },
                      {
                        "position": "2",
                        "positionText": "2",
                        "points": "100",
                        "wins": "2",
                        "Constructor": {
                          "constructorId": "mercedes",
                          "name": "Mercedes"
                        }
                      }
                    ]
                  }]
                }
              }
            }
            """;

        var httpClient = new HttpClient(new FakeHttpHandler(responseJson));
        var repo = new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance);
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStaleness = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
        var sut = new ConstructorStandingService(httpClient, repo, metadataRepo, cacheStaleness, NullLogger<ConstructorStandingService>.Instance);

        // Act
        // BUG: currently throws NullReferenceException on the null Constructor entry
        var act = () => sut.GetConstructorStandingsBySeasonAsync("2026");
        await act.Should().NotThrowAsync(
            "null Constructor entries should be skipped with a warning, not crash the entire fetch");

        // After fix: valid entries should still be returned
        var result = await sut.GetConstructorStandingsBySeasonAsync("2026");
        result.Should().NotBeNull();
        result!.ConstructorStandings.Should().NotBeEmpty(
            "the valid Mercedes entry should still be returned even though the first entry was null");
    }

    // ── Bug #4: int.Parse crash on empty/null Points in ZeroPointer scoring ───
    // ScoringService.cs:276 calls int.Parse(driverStanding.Points) without a null/empty guard.
    // CalculateDriverDraftScoreAsync correctly guards with IsNullOrEmpty; ZeroPointer does not.

    [Fact]
    public async Task ZeroPointerScore_EmptyPointsString_ShouldNotThrow()
    {
        // Arrange
        const string season = "2020";
        const string userId = "user_test";
        const int groupId = 999;
        const string driverId = "test_driver";

        // One-race season with results in (makes season appear "complete")
        _context.Races.Add(new Race { Season = season, Round = "1", RaceName = "Test GP", Date = "2020-12-01" });
        _context.Results.Add(new Result
        {
            Season = season,
            Round = "1",
            DriverId = driverId,
            Position = "1",
            Points = "0",
            IsSprint = false
        });

        // Driver standing with empty Points string — this is the problematic value
        _context.DriverStandings.Add(new DriverStanding
        {
            Season = season,
            Round = "1",
            DriverId = driverId,
            Position = "1",
            PositionText = "1",
            Points = "",          // empty string → int.Parse("") throws FormatException
            Wins = "0",
            ConstructorId = "ferrari"
        });

        // Fresh metadata for all data types — prevents outbound API calls
        var freshFetch = DateTime.UtcNow.AddMinutes(-5);
        _context.DataFetchMetadata.AddRange(
            new DataFetchMetadata { Season = season, DataType = "Races", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "Results", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "DriverStandings", LastFetchedAt = freshFetch, FetchSuccessful = true }
        );
        await _context.SaveChangesAsync();

        // ZeroPointer prediction targeting the driver with empty Points
        var contextFactory = new TestDbContextFactory(_dbOptions);
        await using (var ctx = await contextFactory.CreateDbContextAsync())
        {
            ctx.ZeroPointerPredictions.Add(new ZeroPointerPrediction
            {
                UserId = userId,
                GroupId = groupId,
                DriverIds = new List<string> { driverId },
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        var scoringService = BuildScoringService(contextFactory);

        // Act & Assert
        // BUG: currently throws FormatException from int.Parse("")
        var act = () => scoringService.CalculateZeroPointerScoreAsync(groupId, userId, season);
        await act.Should().NotThrowAsync(
            "empty or unparseable Points should be treated as 0, not crash scoring for the entire group");
    }

    // ── Bug #1: Parallel DbContext access in GetDetailedStandingsAsync ─────────
    // StandingsService.cs:278 uses Task.WhenAll over members, while each member task
    // executes CalculateDetailedScoresAsync which calls shared repositories (same DbContext).
    // RecalculateStandingsAsync explicitly runs sequentially "to avoid DbContext concurrency" —
    // GetDetailedStandingsAsync does not follow the same pattern.
    //
    // NOTE: EF Core in-memory provider may serialize operations and not throw here.
    // The InvalidOperationException ("A second operation was started on this context instance")
    // manifests reliably only under Npgsql with real async IO interleaving.
    // This test verifies correctness; the concurrency fix must be verified against the real DB.

    [Fact]
    public async Task GetDetailedStandings_MultipleMembers_ShouldReturnRankedResults()
    {
        // Arrange
        const string season = "2021";
        const int groupId = 888;

        _context.Groups.Add(new Group
        {
            Id = groupId,
            Name = "Test Group",
            InviteCode = "TEST01",
            AdminUserId = "user_a",
            LockMode = "manual"
        });
        _context.GroupMembers.AddRange(
            new GroupMember { GroupId = groupId, UserId = "user_a", JoinedAt = DateTime.UtcNow },
            new GroupMember { GroupId = groupId, UserId = "user_b", JoinedAt = DateTime.UtcNow }
        );

        // Minimal season data
        _context.Races.Add(new Race { Season = season, Round = "1", RaceName = "Test GP", Date = "2021-03-01" });
        var freshFetch = DateTime.UtcNow.AddMinutes(-5);
        _context.DataFetchMetadata.AddRange(
            new DataFetchMetadata { Season = season, DataType = "Races", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "Results", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "Qualifying", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "DriverStandings", LastFetchedAt = freshFetch, FetchSuccessful = true },
            new DataFetchMetadata { Season = season, DataType = "ConstructorStandings", LastFetchedAt = freshFetch, FetchSuccessful = true }
        );
        await _context.SaveChangesAsync();

        var contextFactory = new TestDbContextFactory(_dbOptions);
        var scoringService = BuildScoringService(contextFactory);

        var standingRepo = new StandingRepository(_context);
        var groupRepo = new GroupRepository(_context, NullLogger<GroupRepository>.Instance);
        var predictionRepo = new PredictionRepository(contextFactory);
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var resultRepo = new ResultRepository(_context, NullLogger<ResultRepository>.Instance);

        var failHttpClient = new HttpClient(new FakeHttpHandler("{}", HttpStatusCode.ServiceUnavailable));
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStaleness = BuildCacheStalenessService();
        var resultService = new ResultService(failHttpClient, resultRepo, metadataRepo, cacheStaleness, NullLogger<ResultService>.Instance);

        var standingsService = new StandingsService(
            standingRepo, groupRepo, predictionRepo, scoringService,
            resultService, resultRepo, metadataRepo, NullLogger<StandingsService>.Instance);

        // Act
        var act = () => standingsService.GetDetailedStandingsAsync(groupId, season);

        // Assert: must complete and return one entry per member
        // If this throws InvalidOperationException, the parallel DbContext bug has been triggered
        await act.Should().NotThrowAsync(
            "GetDetailedStandingsAsync must handle multiple members without concurrent DbContext access");

        var result = await standingsService.GetDetailedStandingsAsync(groupId, season);
        result.Should().HaveCount(2, "one detailed standing per group member");
        result.Select(r => r.Rank).Should().BeEquivalentTo(new[] { 1, 2 },
            "standings must be ranked 1 and 2");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CacheStalenessService BuildCacheStalenessService()
    {
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        return new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);
    }

    private ScoringService BuildScoringService(IDbContextFactory<F1FantasyDbContext> contextFactory)
    {
        var metadataRepo = new DataFetchMetadataRepository(_context, NullLogger<DataFetchMetadataRepository>.Instance);
        var raceRepo = new RaceRepository(_context, NullLogger<RaceRepository>.Instance);
        var cacheStaleness = new CacheStalenessService(metadataRepo, raceRepo, NullLogger<CacheStalenessService>.Instance);

        // Use a client that fails immediately — tests rely on cached/in-memory data only
        var failClient = new HttpClient(new FakeHttpHandler("{}", HttpStatusCode.ServiceUnavailable));

        var driverStandingRepo = new DriverStandingRepository(_context, NullLogger<DriverStandingRepository>.Instance);
        var constructorStandingRepo = new ConstructorStandingRepository(_context, NullLogger<ConstructorStandingRepository>.Instance);
        var resultRepo = new ResultRepository(_context, NullLogger<ResultRepository>.Instance);
        var qualifyingRepo = new QualifyingRepository(_context, NullLogger<QualifyingRepository>.Instance);

        return new ScoringService(
            new PredictionRepository(contextFactory),
            new DriverStandingService(failClient, driverStandingRepo, metadataRepo, cacheStaleness, NullLogger<DriverStandingService>.Instance),
            new ConstructorStandingService(failClient, constructorStandingRepo, metadataRepo, cacheStaleness, NullLogger<ConstructorStandingService>.Instance),
            new ResultService(failClient, resultRepo, metadataRepo, cacheStaleness, NullLogger<ResultService>.Instance),
            new QualifyingService(failClient, qualifyingRepo, metadataRepo, cacheStaleness, NullLogger<QualifyingService>.Instance),
            new RaceService(failClient, raceRepo, metadataRepo, cacheStaleness, NullLogger<RaceService>.Instance)
        );
    }

    public void Dispose() => _context.Dispose();

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;

        public FakeHttpHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
    }
}
