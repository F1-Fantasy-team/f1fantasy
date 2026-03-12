using F1Fantasy.Models;
using F1Fantasy.Repository;
using System.Text.Json;

namespace F1Fantasy.Services;

public class StandingsService
{
    private readonly StandingRepository _standingRepository;
    private readonly GroupRepository _groupRepository;
    private readonly PredictionRepository _predictionRepository;
    private readonly ScoringService _scoringService;
    private readonly ResultService _resultService;
    private readonly ResultRepository _resultRepository;
    private readonly ILogger<StandingsService> _logger;

    public StandingsService(
        StandingRepository standingRepository,
        GroupRepository groupRepository,
        PredictionRepository predictionRepository,
        ScoringService scoringService,
        ResultService resultService,
        ResultRepository resultRepository,
        ILogger<StandingsService> logger)
    {
        _standingRepository = standingRepository;
        _groupRepository = groupRepository;
        _predictionRepository = predictionRepository;
        _scoringService = scoringService;
        _resultService = resultService;
        _resultRepository = resultRepository;
        _logger = logger;
    }

    public async Task<List<Standing>> GetStandingsAsync(int groupId)
    {
        return await _standingRepository.GetStandingsByGroupAsync(groupId);
    }

    public async Task<List<Standing>> GetStandingsWithAutoRecalcAsync(int groupId, string season)
    {
        // Get existing standings
        var existingStandings = await _standingRepository.GetStandingsByGroupAsync(groupId);
        
        // Use ResultService which will intelligently fetch from API only if needed
        var latestRoundWithResults = await _resultService.GetLatestRoundWithResultsAsync(season);
        
        if (latestRoundWithResults == null)
        {
            _logger.LogDebug("No race results available for season {Season}, returning existing standings", season);
            return existingStandings;
        }
        
        // Verify the latest round data is actually in our database
        var latestRoundResults = await _resultRepository.GetByRaceAsync(season, latestRoundWithResults.Value.ToString());
        if (!latestRoundResults.Any())
        {
            _logger.LogWarning("Latest round {Round} reported but no results in DB, forcing recalculation", latestRoundWithResults);
            await RecalculateStandingsAsync(groupId, season);
            return await _standingRepository.GetStandingsByGroupAsync(groupId);
        }
        
        // Determine last calculated round from existing standings
        int? lastCalculatedRound = null;
        if (existingStandings.Any())
        {
            // Try to get the last calculated round from the first user's detailed scores
            var firstStanding = existingStandings.First();
            try
            {
                var detailedStanding = await _scoringService.CalculateDetailedScoresAsync(groupId, firstStanding.UserId, season);
                if (detailedStanding.RoundScores.Any())
                {
                    lastCalculatedRound = detailedStanding.RoundScores.Max(rs => int.Parse(rs.Round));
                    _logger.LogDebug("Last calculated round for group {GroupId}: {Round}", groupId, lastCalculatedRound);
                    
                    // Verify that the calculated round actually has results in DB
                    var calculatedRoundResults = await _resultRepository.GetByRaceAsync(season, lastCalculatedRound.Value.ToString());
                    if (!calculatedRoundResults.Any())
                    {
                        _logger.LogWarning("Last calculated round {Round} has no results in DB, forcing recalculation", lastCalculatedRound);
                        lastCalculatedRound = null; // Force recalc
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine last calculated round, will recalculate");
            }
        }
        
        // Check if recalculation is needed
        bool needsRecalc = lastCalculatedRound == null || lastCalculatedRound < latestRoundWithResults;
        
        if (needsRecalc)
        {
            _logger.LogInformation("Auto-recalculating standings for group {GroupId}, season {Season}. Last calculated: {LastRound}, Latest available: {LatestRound}",
                groupId, season, lastCalculatedRound?.ToString() ?? "none", latestRoundWithResults);
            
            await RecalculateStandingsAsync(groupId, season);
            return await _standingRepository.GetStandingsByGroupAsync(groupId);
        }
        
        _logger.LogDebug("Standings for group {GroupId} are up to date (round {Round})", groupId, lastCalculatedRound);
        return existingStandings;
    }

    public async Task RecalculateStandingsAsync(int groupId, string season)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null) throw new KeyNotFoundException("Group not found");

        // Ensure all F1 data for the season is available before calculating scores
        await _scoringService.EnsureSeasonDataAvailableAsync(season);

        var members = await _groupRepository.GetMembersAsync(groupId);
        
        // Calculate scores sequentially to avoid DbContext concurrency issues
        var standings = new List<(Standing Standing, DateTime? CompletionTime)>();
        foreach (var member in members)
        {
            var categoryScores = await _scoringService.CalculateAllCategoryScoresAsync(groupId, member.UserId, season);
            var totalScore = categoryScores.Values.Sum();
            var completionTime = await GetUserPredictionCompletionTimeAsync(groupId, member.UserId);
            
            standings.Add((new Standing
            {
                GroupId = groupId,
                UserId = member.UserId,
                TotalScore = totalScore,
                CategoryScoresJson = JsonSerializer.Serialize(categoryScores),
                Rank = 0, // Will be set after sorting
                UpdatedAt = DateTime.UtcNow
            }, completionTime));
        }

        // Sort by total score descending, then by completion time ascending (earlier completion = better rank in case of tie)
        var rankedStandings = standings
            .OrderByDescending(s => s.Standing.TotalScore)
            .ThenBy(s => s.CompletionTime)
            .Select(s => s.Standing)
            .ToList();

        // Assign ranks
        for (int i = 0; i < rankedStandings.Count; i++)
        {
            rankedStandings[i].Rank = i + 1;
        }

        // Save to database
        await _standingRepository.UpsertManyAsync(rankedStandings);
    }

    private async Task<DateTime> GetUserPredictionCompletionTimeAsync(int groupId, string userId)
    {
        // Get the latest UpdatedAt timestamp from all prediction types
        // This represents when the user finished their predictions
        var allPredictions = await _predictionRepository.GetAllPredictionsAsync(groupId, userId);
        
        var timestamps = new List<DateTime>();
        
        if (allPredictions.DriverChampionship?.UpdatedAt != null)
            timestamps.Add(allPredictions.DriverChampionship.UpdatedAt.Value);
        else if (allPredictions.DriverChampionship?.CreatedAt != null)
            timestamps.Add(allPredictions.DriverChampionship.CreatedAt);

        if (allPredictions.ConstructorChampionship?.UpdatedAt != null)
            timestamps.Add(allPredictions.ConstructorChampionship.UpdatedAt.Value);
        else if (allPredictions.ConstructorChampionship?.CreatedAt != null)
            timestamps.Add(allPredictions.ConstructorChampionship.CreatedAt);

        if (allPredictions.DriverDraft?.UpdatedAt != null)
            timestamps.Add(allPredictions.DriverDraft.UpdatedAt.Value);
        else if (allPredictions.DriverDraft?.CreatedAt != null)
            timestamps.Add(allPredictions.DriverDraft.CreatedAt);

        if (allPredictions.Destructor?.UpdatedAt != null)
            timestamps.Add(allPredictions.Destructor.UpdatedAt.Value);
        else if (allPredictions.Destructor?.CreatedAt != null)
            timestamps.Add(allPredictions.Destructor.CreatedAt);

        if (allPredictions.MrSaturday?.UpdatedAt != null)
            timestamps.Add(allPredictions.MrSaturday.UpdatedAt.Value);
        else if (allPredictions.MrSaturday?.CreatedAt != null)
            timestamps.Add(allPredictions.MrSaturday.CreatedAt);

        if (allPredictions.ZeroPointer?.UpdatedAt != null)
            timestamps.Add(allPredictions.ZeroPointer.UpdatedAt.Value);
        else if (allPredictions.ZeroPointer?.CreatedAt != null)
            timestamps.Add(allPredictions.ZeroPointer.CreatedAt);

        if (allPredictions.Wildcard?.UpdatedAt != null)
            timestamps.Add(allPredictions.Wildcard.UpdatedAt.Value);
        else if (allPredictions.Wildcard?.CreatedAt != null)
            timestamps.Add(allPredictions.Wildcard.CreatedAt);

        // Return the LATEST timestamp (when they finished all predictions)
        // If no predictions exist, use max value to rank them last in ties
        return timestamps.Any() ? timestamps.Max() : DateTime.MaxValue;
    }

    public async Task<Standing?> GetUserStandingAsync(int groupId, string userId)
    {
        return await _standingRepository.GetByUserAndGroupAsync(groupId, userId);
    }

    public async Task<List<DetailedStanding>> GetDetailedStandingsAsync(int groupId, string season)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null) throw new KeyNotFoundException("Group not found");

        var members = await _groupRepository.GetMembersAsync(groupId);
        
        // Parallelize detailed score calculation for all members
        var detailedTasks = members.Select(async member =>
        {
            var detailed = await _scoringService.CalculateDetailedScoresAsync(groupId, member.UserId, season);
            var completionTime = await GetUserPredictionCompletionTimeAsync(groupId, member.UserId);
            return (Detailed: detailed, CompletionTime: completionTime);
        }).ToList();

        var results = await Task.WhenAll(detailedTasks);

        // Sort by total score descending, then by earliest completion time
        var rankedStandings = results
            .OrderByDescending(r => r.Detailed.TotalScore)
            .ThenBy(r => r.CompletionTime)
            .Select(r => r.Detailed)
            .ToList();

        // Assign ranks
        for (int i = 0; i < rankedStandings.Count; i++)
        {
            rankedStandings[i].Rank = i + 1;
        }

        return rankedStandings;
    }
}
