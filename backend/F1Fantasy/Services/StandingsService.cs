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
    private readonly DataFetchMetadataRepository _metadataRepository;
    private readonly ILogger<StandingsService> _logger;

    public StandingsService(
        StandingRepository standingRepository,
        GroupRepository groupRepository,
        PredictionRepository predictionRepository,
        ScoringService scoringService,
        ResultService resultService,
        ResultRepository resultRepository,
        DataFetchMetadataRepository metadataRepository,
        ILogger<StandingsService> logger)
    {
        _standingRepository = standingRepository;
        _groupRepository = groupRepository;
        _predictionRepository = predictionRepository;
        _scoringService = scoringService;
        _resultService = resultService;
        _resultRepository = resultRepository;
        _metadataRepository = metadataRepository;
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
        
        // CRITICAL FIX: Check if F1 data is NEWER than our standings
        // If standings are older than the F1 data they depend on, we MUST recalculate
        var f1DataNewerThanStandings = await IsF1DataNewerThanStandingsAsync(season, existingStandings);
        
        if (f1DataNewerThanStandings)
        {
            _logger.LogInformation("F1 standings data is newer than calculated standings for group {GroupId}, forcing recalculation", 
                groupId);
            await RecalculateStandingsAsync(groupId, season);
            return await _standingRepository.GetStandingsByGroupAsync(groupId);
        }
        
        // If no results exist yet, nothing to calculate
        var latestRoundWithResults = await _resultService.GetLatestRoundWithResultsAsync(season);
        if (latestRoundWithResults == null)
        {
            _logger.LogDebug("No race results available for season {Season}, returning existing standings", season);
            return existingStandings;
        }

        _logger.LogDebug("Standings for group {GroupId} are up to date", groupId);
        return existingStandings;
    }

    /// <summary>
    /// Check if F1 data (driver/constructor standings) is NEWER than the calculated standings
    /// This is the KEY to automatic recalculation: if the data we depend on has been updated,
    /// our cached standings are stale and must be recalculated
    /// </summary>
    private async Task<bool> IsF1DataNewerThanStandingsAsync(string season, List<Standing> existingStandings)
    {
        try
        {
            // If no standings exist yet, we need to calculate them
            if (!existingStandings.Any())
            {
                _logger.LogDebug("No existing standings found, recalculation needed");
                return true;
            }
            
            // Get the timestamp of when standings were last calculated
            var standingsUpdatedAt = existingStandings.Max(s => s.UpdatedAt);
            
            // Check if any F1 data was fetched AFTER our standings were calculated
            var dataTypes = new[] { "DriverStandings", "ConstructorStandings", "Results", "Qualifying" };
            
            foreach (var dataType in dataTypes)
            {
                var metadata = await _metadataRepository.GetMetadataAsync(season, dataType);
                
                if (metadata != null && metadata.FetchSuccessful)
                {
                    // If F1 data was fetched AFTER we calculated standings, they're stale
                    if (metadata.LastFetchedAt > standingsUpdatedAt)
                    {
                        var timeDiff = metadata.LastFetchedAt - standingsUpdatedAt;
                        _logger.LogInformation("{DataType} was fetched {TimeDiff} after standings were calculated - triggering recalc", 
                            dataType, timeDiff);
                        return true;
                    }
                }
            }
            
            _logger.LogDebug("All F1 data is older than or equal to standings calculation time - no recalc needed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if F1 data is newer than standings, playing it safe and forcing recalc");
            return true; // Play it safe - recalc if we can't determine
        }
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

        // Sequential to avoid concurrent DbContext access on shared repositories
        // (same pattern as RecalculateStandingsAsync)
        var results = new List<(DetailedStanding Detailed, DateTime CompletionTime)>();
        foreach (var member in members)
        {
            var detailed = await _scoringService.CalculateDetailedScoresAsync(groupId, member.UserId, season);
            var completionTime = await GetUserPredictionCompletionTimeAsync(groupId, member.UserId);
            results.Add((detailed, completionTime));
        }

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
