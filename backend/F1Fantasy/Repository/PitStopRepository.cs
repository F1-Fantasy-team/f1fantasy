using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class PitStopRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<PitStopRepository> _logger;

    public PitStopRepository(F1FantasyDbContext context, ILogger<PitStopRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(PitStop pitStop, string season, string round)
    {
        try
        {
            var existing = await _context.PitStops
                .FirstOrDefaultAsync(p => 
                    p.Season == season && 
                    p.Round == round && 
                    p.DriverId == pitStop.DriverId &&
                    p.Stop == pitStop.Stop);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing pit stop: Season {Season}, Round {Round}, Driver {DriverId}, Stop {Stop}", 
                    season, round, pitStop.DriverId, pitStop.Stop);
                
                existing.Lap = pitStop.Lap;
                existing.Time = pitStop.Time;
                existing.Duration = pitStop.Duration;
            }
            else
            {
                _logger.LogDebug("Adding new pit stop: Season {Season}, Round {Round}, Driver {DriverId}, Stop {Stop}, Duration {Duration}", 
                    season, round, pitStop.DriverId, pitStop.Stop, pitStop.Duration);
                
                pitStop.Season = season;
                pitStop.Round = round;
                await _context.PitStops.AddAsync(pitStop);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving pit stop: Season {Season}, Round {Round}, Driver {DriverId}, Stop {Stop}", 
                season, round, pitStop.DriverId, pitStop.Stop);
            throw;
        }
    }

    public async Task AddOrUpdateBatchAsync(IEnumerable<PitStop> pitStops, string season, string round)
    {
        var pitStopList = pitStops.ToList();
        if (!pitStopList.Any()) return;

        try
        {
            // Single query to get all existing pit stops for this season/round
            var existingPitStops = await _context.PitStops
                .Where(p => p.Season == season && p.Round == round)
                .ToListAsync();

            var existingDict = existingPitStops.ToDictionary(p => (p.DriverId, p.Stop));

            var updatedCount = 0;
            var addedCount = 0;

            foreach (var pitStop in pitStopList)
            {
                if (existingDict.TryGetValue((pitStop.DriverId, pitStop.Stop), out var existing))
                {
                    // Update existing
                    existing.Lap = pitStop.Lap;
                    existing.Time = pitStop.Time;
                    existing.Duration = pitStop.Duration;
                    _context.PitStops.Update(existing);
                    updatedCount++;
                }
                else
                {
                    // Add new
                    pitStop.Season = season;
                    pitStop.Round = round;
                    await _context.PitStops.AddAsync(pitStop);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Batch saved pit stops for season {Season}, round {Round}: {Added} added, {Updated} updated",
                season, round, addedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch saving pit stops for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    public async Task<IEnumerable<PitStop>> GetByRaceAsync(string season, string round)
    {
        _logger.LogDebug("Fetching pit stops for season {Season}, round {Round}", season, round);
        var pitStops = await _context.PitStops
            .Where(p => p.Season == season && p.Round == round)
            .ToListAsync();
        
        return pitStops
            .OrderBy(p => int.Parse(p.Lap))
            .ThenBy(p => int.Parse(p.Stop));
    }

    public async Task<IEnumerable<PitStop>> GetByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogDebug("Fetching pit stops for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        var pitStops = await _context.PitStops
            .Where(p => p.Season == season && p.Round == round && p.DriverId == driverId)
            .ToListAsync();
        
        return pitStops.OrderBy(p => int.Parse(p.Stop));
    }

    public async Task<IEnumerable<PitStop>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all pit stops");
        var pitStops = await _context.PitStops.ToListAsync();
        
        return pitStops
            .OrderBy(p => p.Season)
            .ThenBy(p => int.Parse(p.Round))
            .ThenBy(p => int.Parse(p.Lap))
            .ThenBy(p => int.Parse(p.Stop));
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all pit stops from database");
        _context.PitStops.RemoveRange(_context.PitStops);
        await _context.SaveChangesAsync();
    }
}
