using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class ResultRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<ResultRepository> _logger;

    public ResultRepository(F1FantasyDbContext context, ILogger<ResultRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(Result result, string season, string round)
    {
        try
        {
            var existing = await _context.Results
                .FirstOrDefaultAsync(r => 
                    r.Season == season && 
                    r.Round == round && 
                    r.DriverId == result.DriverId &&
                    r.IsSprint == result.IsSprint);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing {ResultType}: Season {Season}, Round {Round}, Driver {DriverId}, Position {Position}", 
                    result.IsSprint ? "sprint result" : "race result", season, round, result.DriverId, result.Position);
                
                existing.Number = result.Number;
                existing.Position = result.Position;
                existing.PositionText = result.PositionText;
                existing.Points = result.Points;
                existing.ConstructorId = result.ConstructorId;
                existing.Grid = result.Grid;
                existing.Laps = result.Laps;
                existing.Status = result.Status;
                existing.Time = result.Time;
                existing.FastestLap = result.FastestLap;
            }
            else
            {
                _logger.LogDebug("Adding new {ResultType}: Season {Season}, Round {Round}, Driver {DriverId}, Position {Position}", 
                    result.IsSprint ? "sprint result" : "race result", season, round, result.DriverId, result.Position);
                
                result.Season = season;
                result.Round = round;
                await _context.Results.AddAsync(result);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving {ResultType}: Season {Season}, Round {Round}, Driver {DriverId}", 
                result.IsSprint ? "sprint result" : "race result", season, round, result.DriverId);
            throw;
        }
    }

    public async Task<IEnumerable<Result>> GetBySeasonAsync(string season)
    {
        _logger.LogDebug("Fetching results for season {Season}", season);
        var results = await _context.Results
            .Where(r => r.Season == season && !r.IsSprint)
            .ToListAsync();
        
        return results
            .OrderBy(r => int.Parse(r.Round))
            .ThenBy(r => int.Parse(r.Position));
    }

    public async Task<IEnumerable<Result>> GetSprintResultsBySeasonAsync(string season)
    {
        _logger.LogDebug("Fetching sprint results for season {Season}", season);
        var results = await _context.Results
            .Where(r => r.Season == season && r.IsSprint)
            .ToListAsync();
        
        return results
            .OrderBy(r => int.Parse(r.Round))
            .ThenBy(r => int.Parse(r.Position));
    }

    public async Task<IEnumerable<Result>> GetByRaceAsync(string season, string round)
    {
        _logger.LogDebug("Fetching results for season {Season}, round {Round}", season, round);
        var results = await _context.Results
            .Where(r => r.Season == season && r.Round == round && !r.IsSprint)
            .ToListAsync();
        
        return results.OrderBy(r => int.Parse(r.Position));
    }

    public async Task<IEnumerable<Result>> GetSprintResultsByRaceAsync(string season, string round)
    {
        _logger.LogDebug("Fetching sprint results for season {Season}, round {Round}", season, round);
        var results = await _context.Results
            .Where(r => r.Season == season && r.Round == round && r.IsSprint)
            .ToListAsync();
        
        return results.OrderBy(r => int.Parse(r.Position));
    }

    public async Task<Result?> GetByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogDebug("Fetching result for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        return await _context.Results
            .FirstOrDefaultAsync(r => 
                r.Season == season && 
                r.Round == round && 
                r.DriverId == driverId);
    }

    public async Task<IEnumerable<Result>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all results");
        var results = await _context.Results.ToListAsync();
        
        return results
            .OrderBy(r => r.Season)
            .ThenBy(r => int.Parse(r.Round))
            .ThenBy(r => int.Parse(r.Position));
    }

    public async Task<int?> GetLatestRoundWithResultsAsync(string season)
    {
        _logger.LogDebug("Getting latest round with results for season {Season}", season);
        
        var rounds = await _context.Results
            .Where(r => r.Season == season && !r.IsSprint)
            .Select(r => r.Round)
            .Distinct()
            .ToListAsync();
        
        if (!rounds.Any())
        {
            _logger.LogDebug("No results found for season {Season}", season);
            return null;
        }
        
        var latestRound = rounds.Max(r => int.Parse(r));
        _logger.LogDebug("Latest round with results for season {Season}: {Round}", season, latestRound);
        return latestRound;
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all results from database");
        _context.Results.RemoveRange(_context.Results);
        await _context.SaveChangesAsync();
    }
}
