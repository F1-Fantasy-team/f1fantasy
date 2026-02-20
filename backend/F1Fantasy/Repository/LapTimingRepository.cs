using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class LapTimingRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<LapTimingRepository> _logger;

    public LapTimingRepository(F1FantasyDbContext context, ILogger<LapTimingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(LapTiming lapTiming, string season, string round)
    {
        try
        {
            var existing = await _context.LapTimings
                .FirstOrDefaultAsync(l => 
                    l.Season == season && 
                    l.Round == round && 
                    l.LapNumber == lapTiming.LapNumber &&
                    l.DriverId == lapTiming.DriverId);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing lap timing: Season {Season}, Round {Round}, Lap {Lap}, Driver {DriverId}", 
                    season, round, lapTiming.LapNumber, lapTiming.DriverId);
                
                existing.Position = lapTiming.Position;
                existing.Time = lapTiming.Time;
            }
            else
            {
                _logger.LogDebug("Adding new lap timing: Season {Season}, Round {Round}, Lap {Lap}, Driver {DriverId}, Position {Position}", 
                    season, round, lapTiming.LapNumber, lapTiming.DriverId, lapTiming.Position);
                
                lapTiming.Season = season;
                lapTiming.Round = round;
                await _context.LapTimings.AddAsync(lapTiming);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving lap timing: Season {Season}, Round {Round}, Lap {Lap}, Driver {DriverId}", 
                season, round, lapTiming.LapNumber, lapTiming.DriverId);
            throw;
        }
    }

    public async Task<IEnumerable<LapTiming>> GetByRaceAsync(string season, string round)
    {
        _logger.LogDebug("Fetching lap timings for season {Season}, round {Round}", season, round);
        var lapTimings = await _context.LapTimings
            .Where(l => l.Season == season && l.Round == round)
            .ToListAsync();
        
        return lapTimings
            .OrderBy(l => int.Parse(l.LapNumber))
            .ThenBy(l => int.Parse(l.Position));
    }

    public async Task<IEnumerable<LapTiming>> GetByLapAsync(string season, string round, string lapNumber)
    {
        _logger.LogDebug("Fetching lap timings for season {Season}, round {Round}, lap {Lap}", 
            season, round, lapNumber);
        var lapTimings = await _context.LapTimings
            .Where(l => l.Season == season && l.Round == round && l.LapNumber == lapNumber)
            .ToListAsync();
        
        return lapTimings.OrderBy(l => int.Parse(l.Position));
    }

    public async Task<IEnumerable<LapTiming>> GetByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogDebug("Fetching lap timings for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        var lapTimings = await _context.LapTimings
            .Where(l => l.Season == season && l.Round == round && l.DriverId == driverId)
            .ToListAsync();
        
        return lapTimings.OrderBy(l => int.Parse(l.LapNumber));
    }

    public async Task<IEnumerable<LapTiming>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all lap timings");
        var lapTimings = await _context.LapTimings.ToListAsync();
        
        return lapTimings
            .OrderBy(l => l.Season)
            .ThenBy(l => int.Parse(l.Round))
            .ThenBy(l => int.Parse(l.LapNumber))
            .ThenBy(l => int.Parse(l.Position));
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all lap timings from database");
        _context.LapTimings.RemoveRange(_context.LapTimings);
        await _context.SaveChangesAsync();
    }
}
