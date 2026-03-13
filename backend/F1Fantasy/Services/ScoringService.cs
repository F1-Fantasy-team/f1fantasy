using F1Fantasy.Models;
using F1Fantasy.Repository;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Services;

public class ScoringService
{
    private readonly PredictionRepository _predictionRepository;
    private readonly DriverStandingService _driverStandingService;
    private readonly ConstructorStandingService _constructorStandingService;
    private readonly ResultService _resultService;
    private readonly QualifyingService _qualifyingService;
    private readonly RaceService _raceService;

    // Scoring constants
    private const int CHAMPIONSHIP_EXACT_MATCH_POINTS = 10;
    private const int CHAMPIONSHIP_POSITION_PENALTY = -2;
    private const int DESTRUCTOR_DNF_POINTS = 20;
    private const int MR_SATURDAY_QUALI_WIN_POINTS = 10;
    private const int ZERO_POINTER_POINTS = 100;
    private const int ZERO_POINTER_PENALTY = -20;
    
    // Mr Saturday logic version for cache invalidation
    // Version 1 = old pole position logic (buggy)
    // Version 2 = new teammate comparison logic (correct)
    private const int MR_SATURDAY_LOGIC_VERSION = 2;

    public ScoringService(
        PredictionRepository predictionRepository,
        DriverStandingService driverStandingService,
        ConstructorStandingService constructorStandingService,
        ResultService resultService,
        QualifyingService qualifyingService,
        RaceService raceService)
    {
        _predictionRepository = predictionRepository;
        _driverStandingService = driverStandingService;
        _constructorStandingService = constructorStandingService;
        _resultService = resultService;
        _qualifyingService = qualifyingService;
        _raceService = raceService;
    }

    /// <summary>
    /// Ensures all required F1 data for a season is available (fetches from API if not cached)
    /// </summary>
    public async Task EnsureSeasonDataAvailableAsync(string season)
    {
        // Fetch driver standings (will use cache if available, otherwise API)
        await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(season);
        
        // Fetch constructor standings
        await _constructorStandingService.GetConstructorStandingsBySeasonCachedAsync(season);
        
        // Fetch qualifying data
        await _qualifyingService.GetQualifyingBySeasonCachedAsync(season);
        
        // Fetch race results
        await _resultService.GetResultsBySeasonCachedAsync(season);
    }

    public async Task<int> CalculateConstructorChampionshipScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetConstructorChampionshipAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var standingsList = await _constructorStandingService.GetConstructorStandingsBySeasonCachedAsync(season);
        if (standingsList?.ConstructorStandings == null || !standingsList.ConstructorStandings.Any())
            return 0;

        // Build actual ranking with all constructors
        // Those with numeric position come first, then those with positionText="-" are treated as position 22
        var actualRanking = standingsList.ConstructorStandings
            .Select(s => new {
                ConstructorId = s.Constructor?.ConstructorId,
                Position = int.TryParse(s.Position, out var pos) ? pos : 22
            })
            .Where(x => x.ConstructorId != null)
            .OrderBy(x => x.Position)
            .Select(x => x.ConstructorId!)
            .ToList();

        return CalculateChampionshipScore(prediction.RankedConstructorIds, actualRanking);
    }

    public async Task<int> CalculateDriverChampionshipScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetDriverChampionshipAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var standingsList = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(season);
        if (standingsList?.DriverStandings == null || !standingsList.DriverStandings.Any())
            return 0;

        // Build actual ranking with all drivers
        // Those with numeric position come first, then those with positionText="-" are treated as position 22
        var actualRanking = standingsList.DriverStandings
            .Select(s => new {
                DriverId = s.Driver?.DriverId,
                Position = int.TryParse(s.Position, out var pos) ? pos : 22
            })
            .Where(x => x.DriverId != null)
            .OrderBy(x => x.Position)
            .Select(x => x.DriverId!)
            .ToList();

        return CalculateChampionshipScore(prediction.RankedDriverIds, actualRanking);
    }

    public async Task<int> CalculateDriverDraftScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetDriverDraftAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var standingsList = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(season);
        if (standingsList?.DriverStandings == null) return 0;
        
        int totalPoints = 0;

        if (prediction.Driver1Id != null)
        {
            var driver1Standing = standingsList.DriverStandings.FirstOrDefault(s => s.Driver?.DriverId == prediction.Driver1Id);
            if (driver1Standing != null && !string.IsNullOrEmpty(driver1Standing.Points))
            {
                totalPoints += int.Parse(driver1Standing.Points);
            }
        }

        if (prediction.Driver2Id != null)
        {
            var driver2Standing = standingsList.DriverStandings.FirstOrDefault(s => s.Driver?.DriverId == prediction.Driver2Id);
            if (driver2Standing != null && !string.IsNullOrEmpty(driver2Standing.Points))
            {
                totalPoints += int.Parse(driver2Standing.Points);
            }
        }

        return totalPoints;
    }

    public async Task<int> CalculateDestructorScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetDestructorAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var racesWithResults = await _resultService.GetResultsBySeasonCachedAsync(season);
        if (racesWithResults == null || !racesWithResults.Any()) return 0;

        var allResults = racesWithResults
            .Where(r => r.Results != null)
            .SelectMany(r => r.Results!)
            .ToList();
        
        int totalScore = 0;

        // Count DNFs for each predicted driver
        if (prediction.Driver1Id != null)
        {
            var dnfCount = allResults.Count(r => r.Driver?.DriverId == prediction.Driver1Id && IsDNF(r.Status));
            totalScore += dnfCount * DESTRUCTOR_DNF_POINTS;
        }

        if (prediction.Driver2Id != null)
        {
            var dnfCount = allResults.Count(r => r.Driver?.DriverId == prediction.Driver2Id && IsDNF(r.Status));
            totalScore += dnfCount * DESTRUCTOR_DNF_POINTS;
        }

        return totalScore;
    }

    public async Task<int> CalculateMrSaturdayScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetMrSaturdayAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var racesWithQualifying = await _qualifyingService.GetQualifyingBySeasonCachedAsync(season);
        if (racesWithQualifying == null || !racesWithQualifying.Any()) return 0;

        int totalScore = 0;

        // Calculate score for each race with qualifying data
        foreach (var race in racesWithQualifying.Where(r => r.QualifyingResults != null && r.QualifyingResults.Any()))
        {
            var grid = BuildQualifyingGrid(race.QualifyingResults!);
            
            // Check Driver1
            if (prediction.Driver1Id != null)
            {
                var driver1 = grid.FirstOrDefault(g => g.DriverId == prediction.Driver1Id);
                if (driver1 != null)
                {
                    var teammate = GetTeammateForDriver(driver1.DriverId, driver1.ConstructorId, grid);
                    if (teammate != null)
                    {
                        var comparison = CompareTeammates(driver1, teammate);
                        if (comparison < 0) // Driver1 wins
                        {
                            totalScore += MR_SATURDAY_QUALI_WIN_POINTS;
                        }
                        // If comparison == 0 (tie), no points awarded
                    }
                    else
                    {
                        // No teammate found (DNS/absent) - driver gets points by default
                        totalScore += MR_SATURDAY_QUALI_WIN_POINTS;
                    }
                }
            }

            // Check Driver2
            if (prediction.Driver2Id != null)
            {
                var driver2 = grid.FirstOrDefault(g => g.DriverId == prediction.Driver2Id);
                if (driver2 != null)
                {
                    var teammate = GetTeammateForDriver(driver2.DriverId, driver2.ConstructorId, grid);
                    if (teammate != null)
                    {
                        var comparison = CompareTeammates(driver2, teammate);
                        if (comparison < 0) // Driver2 wins
                        {
                            totalScore += MR_SATURDAY_QUALI_WIN_POINTS;
                        }
                    }
                    else
                    {
                        // No teammate found - driver gets points by default
                        totalScore += MR_SATURDAY_QUALI_WIN_POINTS;
                    }
                }
            }
        }

        return totalScore;
    }

    public async Task<int> CalculateZeroPointerScoreAsync(int groupId, string userId, string season)
    {
        var prediction = await _predictionRepository.GetZeroPointerAsync(groupId, userId);
        if (prediction == null || prediction.DriverIds == null || !prediction.DriverIds.Any()) return 0;

        // Zero Pointer should only score at the END OF THE SEASON
        // Get total races for the season
        var allRaces = await _raceService.GetRacesForSeasonAsync(season);
        var totalRaces = allRaces.Count();
        
        // Get latest round with results
        var latestRound = await _resultService.GetLatestRoundWithResultsAsync(season);
        
        // If season is not complete, return 0 (no points yet)
        if (!latestRound.HasValue || latestRound.Value < totalRaces)
        {
            return 0;
        }

        // Season is complete - calculate Zero Pointer scores
        // Use cache-first method for better performance
        var standingsList = await _driverStandingService.GetDriverStandingsBySeasonCachedAsync(season);
        if (standingsList?.DriverStandings == null) return 0;
        
        int totalScore = 0;

        // Check each predicted driver
        foreach (var driverId in prediction.DriverIds)
        {
            var driverStanding = standingsList.DriverStandings.FirstOrDefault(s => s.Driver?.DriverId == driverId);
            if (driverStanding != null)
            {
                var points = int.Parse(driverStanding.Points);
                if (points == 0)
                {
                    // Correct prediction: driver has 0 points
                    totalScore += ZERO_POINTER_POINTS;
                }
                else
                {
                    // Incorrect prediction: driver has points
                    totalScore += ZERO_POINTER_PENALTY;
                }
            }
        }

        return totalScore;
    }

    public async Task<int> CalculateWildcardScoreAsync(int groupId, string userId)
    {
        var prediction = await _predictionRepository.GetWildcardAsync(groupId, userId);
        if (prediction == null || prediction.Fullfilled != true) return 0;

        return prediction.PointsPotential ?? 0;
    }

    public async Task<Dictionary<string, int>> CalculateAllCategoryScoresAsync(int groupId, string userId, string season)
    {
        // Calculate scores sequentially to avoid DbContext concurrency issues
        // The async/await pattern already provides adequate concurrency at the database level
        var constructorChamp = await CalculateConstructorChampionshipScoreAsync(groupId, userId, season);
        var driverChamp = await CalculateDriverChampionshipScoreAsync(groupId, userId, season);
        var driverDraft = await CalculateDriverDraftScoreAsync(groupId, userId, season);
        var destructor = await CalculateDestructorScoreAsync(groupId, userId, season);
        var mrSaturday = await CalculateMrSaturdayScoreAsync(groupId, userId, season);
        var zeroPointer = await CalculateZeroPointerScoreAsync(groupId, userId, season);
        var wildcard = await CalculateWildcardScoreAsync(groupId, userId);

        return new Dictionary<string, int>
        {
            ["constructorChampionship"] = constructorChamp,
            ["driverChampionship"] = driverChamp,
            ["driverDraft"] = driverDraft,
            ["destructor"] = destructor,
            ["mrSaturday"] = mrSaturday,
            ["zeroPointer"] = zeroPointer,
            ["wildcard"] = wildcard
        };
    }

    private int CalculateChampionshipScore(List<string> predicted, List<string> actual)
    {
        int score = 0;
        const int BASELINE_POINTS = 20;

        for (int i = 0; i < predicted.Count; i++)
        {
            // Each prediction starts with 20 points baseline
            int predictionScore = BASELINE_POINTS;

            if (i < actual.Count && predicted[i] == actual[i])
            {
                // Exact match: baseline + bonus
                predictionScore += CHAMPIONSHIP_EXACT_MATCH_POINTS;
            }
            else
            {
                // Find actual position of predicted driver/constructor
                int actualPosition = actual.IndexOf(predicted[i]);
                if (actualPosition != -1)
                {
                    // Calculate position delta and deduct penalty
                    int delta = Math.Abs(i - actualPosition);
                    predictionScore += delta * CHAMPIONSHIP_POSITION_PENALTY; // Negative penalty
                }
                else
                {
                    // Driver/constructor not in the actual standings - assume worst position (22)
                    int delta = Math.Abs(i - 21); // 21 because index is 0-based, position 22
                    predictionScore += delta * CHAMPIONSHIP_POSITION_PENALTY;
                }
            }

            score += predictionScore;
        }

        return score;
    }

    private bool IsDNF(string? status)
    {
        if (string.IsNullOrEmpty(status)) return false;
        
        var dnfKeywords = new[] { 
            "Accident", "Collision", "Spun off", "Retired", "Engine", "Gearbox", 
            "Transmission", "Clutch", "Hydraulics", "Electrical", "Suspension",
            "Brakes", "Differential", "Overheating", "Mechanical", "Tyre", "Puncture",
            "Driveshaft", "Fuel", "Oil", "Water", "Vibrations", "Withdrew", "Did not start"
        };

        return dnfKeywords.Any(keyword => status.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    // Calculate detailed scores with round-by-round breakdown
    public async Task<DetailedStanding> CalculateDetailedScoresAsync(int groupId, string userId, string season)
    {
        // Use service which handles API fetch + caching automatically
        var racesWithResults = await _resultService.GetResultsBySeasonAsync(season);
        if (racesWithResults == null)
        {
            return new DetailedStanding
            {
                UserId = userId,
                GroupId = groupId,
                TotalScore = 0,
                Rank = 0,
                CategoryTotals = new Dictionary<string, int>(),
                RoundScores = new List<RoundScore>()
            };
        }
        
        // Get unique races by round
        var racesByRound = racesWithResults
            .OrderBy(r => int.Parse(r.Round))
            .ToList();

        var roundScores = new List<RoundScore>();
        var categoryTotals = new Dictionary<string, int>
        {
            ["DriverChampionship"] = 0,
            ["ConstructorChampionship"] = 0,
            ["DriverDraft"] = 0,
            ["Destructor"] = 0,
            ["MrSaturday"] = 0,
            ["ZeroPointer"] = 0,
            ["Wildcard"] = 0
        };

        int cumulativeScore = 0;

        foreach (var raceInfo in racesByRound)
        {
            var roundScore = new RoundScore
            {
                Round = raceInfo.Round,
                RaceName = $"Round {raceInfo.Round}",
                Date = null,
                CategoryScores = new Dictionary<string, int>()
            };

            // Parallelize per-round score calculations
            var destructorTask = CalculateDestructorScoreForRoundAsync(groupId, userId, season, raceInfo.Round);
            var mrSaturdayTask = CalculateMrSaturdayScoreForRoundAsync(groupId, userId, season, raceInfo.Round);
            var driverDraftTask = CalculateDriverDraftScoreForRoundAsync(groupId, userId, season, raceInfo.Round);

            await Task.WhenAll(destructorTask, mrSaturdayTask, driverDraftTask);

            var destructorPoints = await destructorTask;
            var mrSaturdayPoints = await mrSaturdayTask;
            var driverDraftPoints = await driverDraftTask;

            roundScore.CategoryScores["Destructor"] = destructorPoints;
            roundScore.CategoryScores["MrSaturday"] = mrSaturdayPoints;
            roundScore.CategoryScores["DriverDraft"] = driverDraftPoints;

            // Championship predictions are calculated at season end (not per round)
            roundScore.CategoryScores["DriverChampionship"] = 0;
            roundScore.CategoryScores["ConstructorChampionship"] = 0;
            roundScore.CategoryScores["ZeroPointer"] = 0;
            roundScore.CategoryScores["Wildcard"] = 0;

            categoryTotals["Destructor"] += destructorPoints;
            categoryTotals["MrSaturday"] += mrSaturdayPoints;
            categoryTotals["DriverDraft"] += driverDraftPoints;

            cumulativeScore += destructorPoints + mrSaturdayPoints + driverDraftPoints;
            roundScore.CumulativeScore = cumulativeScore;

            roundScores.Add(roundScore);
        }

        // Add end-of-season categories to the last round (if races exist)
        if (roundScores.Any())
        {
            // Parallelize season-end score calculations
            var driverChampTask = CalculateDriverChampionshipScoreAsync(groupId, userId, season);
            var constructorChampTask = CalculateConstructorChampionshipScoreAsync(groupId, userId, season);
            var zeroPointerTask = CalculateZeroPointerScoreAsync(groupId, userId, season);
            var wildcardTask = CalculateWildcardScoreAsync(groupId, userId);
            var driverDraftSeasonTask = CalculateDriverDraftScoreAsync(groupId, userId, season);

            await Task.WhenAll(driverChampTask, constructorChampTask, zeroPointerTask, wildcardTask, driverDraftSeasonTask);

            var driverChampPoints = await driverChampTask;
            var constructorChampPoints = await constructorChampTask;
            var zeroPointerPoints = await zeroPointerTask;
            var wildcardPoints = await wildcardTask;
            var driverDraftSeasonTotal = await driverDraftSeasonTask;

            var lastRound = roundScores[^1];
            lastRound.CategoryScores["DriverChampionship"] = driverChampPoints;
            lastRound.CategoryScores["ConstructorChampionship"] = constructorChampPoints;
            lastRound.CategoryScores["ZeroPointer"] = zeroPointerPoints;
            lastRound.CategoryScores["Wildcard"] = wildcardPoints;
            lastRound.CategoryScores["DriverDraft"] = driverDraftSeasonTotal; // Override accumulated value

            categoryTotals["DriverChampionship"] = driverChampPoints;
            categoryTotals["ConstructorChampionship"] = constructorChampPoints;
            categoryTotals["ZeroPointer"] = zeroPointerPoints;
            categoryTotals["Wildcard"] = wildcardPoints;
            categoryTotals["DriverDraft"] = driverDraftSeasonTotal; // Use season total, not accumulated per-round

            lastRound.CumulativeScore += driverChampPoints + constructorChampPoints + zeroPointerPoints + wildcardPoints;
        }

        return new DetailedStanding
        {
            UserId = userId,
            GroupId = groupId,
            TotalScore = categoryTotals.Values.Sum(),
            Rank = 0, // Will be set by caller
            CategoryTotals = categoryTotals,
            RoundScores = roundScores
        };
    }

    private async Task<int> CalculateDestructorScoreForRoundAsync(int groupId, string userId, string season, string round)
    {
        var prediction = await _predictionRepository.GetDestructorAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var racesWithResults = await _resultService.GetResultsBySeasonCachedAsync(season);
        if (racesWithResults == null) return 0;

        var roundRace = racesWithResults.FirstOrDefault(r => r.Round == round);
        if (roundRace?.Results == null) return 0;

        int points = 0;

        if (prediction.Driver1Id != null)
        {
            var driver1Result = roundRace.Results.FirstOrDefault(r => r.Driver?.DriverId == prediction.Driver1Id);
            if (driver1Result != null && IsDNF(driver1Result.Status))
            {
                points += DESTRUCTOR_DNF_POINTS;
            }
        }

        if (prediction.Driver2Id != null)
        {
            var driver2Result = roundRace.Results.FirstOrDefault(r => r.Driver?.DriverId == prediction.Driver2Id);
            if (driver2Result != null && IsDNF(driver2Result.Status))
            {
                points += DESTRUCTOR_DNF_POINTS;
            }
        }

        return points;
    }

    private async Task<int> CalculateMrSaturdayScoreForRoundAsync(int groupId, string userId, string season, string round)
    {
        var prediction = await _predictionRepository.GetMrSaturdayAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var racesWithQualifying = await _qualifyingService.GetQualifyingBySeasonCachedAsync(season);
        if (racesWithQualifying == null) return 0;

        var roundRace = racesWithQualifying.FirstOrDefault(r => r.Round == round);
        if (roundRace?.QualifyingResults == null || !roundRace.QualifyingResults.Any()) return 0;

        int points = 0;
        var grid = BuildQualifyingGrid(roundRace.QualifyingResults);

        // Check Driver1
        if (prediction.Driver1Id != null)
        {
            var driver1 = grid.FirstOrDefault(g => g.DriverId == prediction.Driver1Id);
            if (driver1 != null)
            {
                var teammate = GetTeammateForDriver(driver1.DriverId, driver1.ConstructorId, grid);
                if (teammate != null)
                {
                    var comparison = CompareTeammates(driver1, teammate);
                    if (comparison < 0) // Driver1 wins
                    {
                        points += MR_SATURDAY_QUALI_WIN_POINTS;
                    }
                }
                else
                {
                    // No teammate found - driver gets points by default
                    points += MR_SATURDAY_QUALI_WIN_POINTS;
                }
            }
        }

        // Check Driver2
        if (prediction.Driver2Id != null)
        {
            var driver2 = grid.FirstOrDefault(g => g.DriverId == prediction.Driver2Id);
            if (driver2 != null)
            {
                var teammate = GetTeammateForDriver(driver2.DriverId, driver2.ConstructorId, grid);
                if (teammate != null)
                {
                    var comparison = CompareTeammates(driver2, teammate);
                    if (comparison < 0) // Driver2 wins
                    {
                        points += MR_SATURDAY_QUALI_WIN_POINTS;
                    }
                }
                else
                {
                    // No teammate found - driver gets points by default
                    points += MR_SATURDAY_QUALI_WIN_POINTS;
                }
            }
        }

        return points;
    }

    private async Task<int> CalculateDriverDraftScoreForRoundAsync(int groupId, string userId, string season, string round)
    {
        var prediction = await _predictionRepository.GetDriverDraftAsync(groupId, userId);
        if (prediction == null) return 0;

        // Use cache-first method for better performance
        var racesWithResults = await _resultService.GetResultsBySeasonCachedAsync(season);
        if (racesWithResults == null) return 0;

        var roundRace = racesWithResults.FirstOrDefault(r => r.Round == round);
        if (roundRace?.Results == null) return 0;

        int points = 0;

        if (prediction.Driver1Id != null)
        {
            var driver1Result = roundRace.Results.FirstOrDefault(r => r.Driver?.DriverId == prediction.Driver1Id);
            if (driver1Result != null && !string.IsNullOrEmpty(driver1Result.Points))
            {
                points += int.Parse(driver1Result.Points);
            }
        }

        if (prediction.Driver2Id != null)
        {
            var driver2Result = roundRace.Results.FirstOrDefault(r => r.Driver?.DriverId == prediction.Driver2Id);
            if (driver2Result != null && !string.IsNullOrEmpty(driver2Result.Points))
            {
                points += int.Parse(driver2Result.Points);
            }
        }

        return points;
    }

    // ==================== Mr Saturday Helper Methods ====================
    
    /// <summary>
    /// Represents a driver's position on the qualifying grid with all session times
    /// </summary>
    public class QualifyingGridPosition
    {
        public required string DriverId { get; set; }
        public required string ConstructorId { get; set; }
        public int GridPosition { get; set; }
        public TimeSpan? Q1Time { get; set; }
        public TimeSpan? Q2Time { get; set; }
        public TimeSpan? Q3Time { get; set; }
        
        /// <summary>
        /// Gets the highest qualifying stage reached (3 = Q3, 2 = Q2, 1 = Q1, 0 = none)
        /// </summary>
        public int HighestStageReached => 
            Q3Time.HasValue ? 3 : 
            Q2Time.HasValue ? 2 : 
            Q1Time.HasValue ? 1 : 0;
    }
    
    /// <summary>
    /// Builds the qualifying grid from API results, sorted by qualification performance
    /// </summary>
    public static List<QualifyingGridPosition> BuildQualifyingGrid(List<Qualifying> qualifyingResults)
    {
        var grid = new List<QualifyingGridPosition>();
        
        foreach (var result in qualifyingResults)
        {
            grid.Add(new QualifyingGridPosition
            {
                DriverId = result.DriverId,
                ConstructorId = result.ConstructorId,
                Q1Time = ParseQualifyingTime(result.Q1),
                Q2Time = ParseQualifyingTime(result.Q2),
                Q3Time = ParseQualifyingTime(result.Q3),
                GridPosition = int.TryParse(result.Position, out var pos) ? pos : 99
            });
        }
        
        // Sort by grid position (which already comes from API in correct order)
        return grid.OrderBy(g => g.GridPosition).ToList();
    }
    
    /// <summary>
    /// Compares two teammates based on qualifying performance with tiebreaker logic
    /// </summary>
    /// <returns>
    /// -1 if driver1 beats driver2,
    /// 0 if tie (same times in all stages),
    /// +1 if driver2 beats driver1
    /// </returns>
    public static int CompareTeammates(QualifyingGridPosition driver1, QualifyingGridPosition driver2)
    {
        // Determine highest common stage both drivers reached
        int maxStage = Math.Min(driver1.HighestStageReached, driver2.HighestStageReached);
        
        // If one driver didn't set any time (DNS), other driver wins
        if (driver1.HighestStageReached == 0 && driver2.HighestStageReached > 0) return 1;
        if (driver2.HighestStageReached == 0 && driver1.HighestStageReached > 0) return -1;
        if (driver1.HighestStageReached == 0 && driver2.HighestStageReached == 0) return 0;
        
        // If one driver reached a higher stage, they win automatically
        if (driver1.HighestStageReached > driver2.HighestStageReached) return -1;
        if (driver2.HighestStageReached > driver1.HighestStageReached) return 1;
        
        // Both reached same stage - compare times with tiebreaker logic
        // Start from highest stage and work backwards (Q3 -> Q2 -> Q1)
        
        // Try Q3 comparison if both reached Q3
        if (maxStage >= 3 && driver1.Q3Time.HasValue && driver2.Q3Time.HasValue)
        {
            var q3Comparison = driver1.Q3Time.Value.CompareTo(driver2.Q3Time.Value);
            if (q3Comparison != 0) return q3Comparison; // Different Q3 times
            // Q3 times are equal, fall through to Q2 tiebreaker
        }
        
        // Try Q2 comparison if both reached Q2 (and Q3 was tied or not applicable)
        if (maxStage >= 2 && driver1.Q2Time.HasValue && driver2.Q2Time.HasValue)
        {
            var q2Comparison = driver1.Q2Time.Value.CompareTo(driver2.Q2Time.Value);
            if (q2Comparison != 0) return q2Comparison; // Different Q2 times
            // Q2 times are equal, fall through to Q1 tiebreaker
        }
        
        // Try Q1 comparison if both set Q1 times (and Q3/Q2 were tied or not applicable)
        if (maxStage >= 1 && driver1.Q1Time.HasValue && driver2.Q1Time.HasValue)
        {
            var q1Comparison = driver1.Q1Time.Value.CompareTo(driver2.Q1Time.Value);
            if (q1Comparison != 0) return q1Comparison; // Different Q1 times
            // Q1 times are equal - complete tie
        }
        
        // All times identical - tie, no points awarded
        return 0;
    }
    
    /// <summary>
    /// Finds the teammate for a given driver in the qualifying grid
    /// </summary>
    /// <returns>Teammate's grid position, or null if no teammate found</returns>
    public static QualifyingGridPosition? GetTeammateForDriver(
        string driverId, 
        string constructorId, 
        List<QualifyingGridPosition> grid)
    {
        return grid.FirstOrDefault(g => 
            g.ConstructorId == constructorId && 
            g.DriverId != driverId);
    }
    
    /// <summary>
    /// Parses a qualifying lap time string (e.g., "1:19.478") into a TimeSpan
    /// </summary>
    private static TimeSpan? ParseQualifyingTime(string? timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
            return null;
        
        var parts = timeStr.Split(':');
        if (parts.Length != 2)
            return null;
        
        if (!int.TryParse(parts[0], out var minutes))
            return null;
        
        if (!double.TryParse(parts[1], out var seconds))
            return null;
        
        return TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
    }
}
