using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class QualifyingRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<QualifyingRepository> _logger;

    public QualifyingRepository(F1FantasyDbContext context, ILogger<QualifyingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(Qualifying qualifying, string season, string round)
    {
        try
        {
            var existing = await _context.Qualifyings
                .FirstOrDefaultAsync(q => 
                    q.Season == season && 
                    q.Round == round && 
                    q.DriverId == qualifying.DriverId);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing qualifying: Season {Season}, Round {Round}, Driver {DriverId}, Position {Position}", 
                    season, round, qualifying.DriverId, qualifying.Position);
                
                existing.Number = qualifying.Number;
                existing.Position = qualifying.Position;
                existing.ConstructorId = qualifying.ConstructorId;
                existing.Q1 = qualifying.Q1;
                existing.Q2 = qualifying.Q2;
                existing.Q3 = qualifying.Q3;
            }
            else
            {
                _logger.LogDebug("Adding new qualifying: Season {Season}, Round {Round}, Driver {DriverId}, Position {Position}", 
                    season, round, qualifying.DriverId, qualifying.Position);
                
                qualifying.Season = season;
                qualifying.Round = round;
                await _context.Qualifyings.AddAsync(qualifying);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving qualifying: Season {Season}, Round {Round}, Driver {DriverId}", 
                season, round, qualifying.DriverId);
            throw;
        }
    }

    public async Task AddOrUpdateBatchAsync(IEnumerable<Qualifying> qualifyings, string season, string round)
    {
        var qualifyingList = qualifyings.ToList();
        if (!qualifyingList.Any()) return;

        try
        {
            var driverIds = qualifyingList.Select(q => q.DriverId).ToList();

            // Single query to get all existing qualifyings for this season/round
            var existingQualifyings = await _context.Qualifyings
                .Where(q => q.Season == season && q.Round == round && driverIds.Contains(q.DriverId))
                .ToDictionaryAsync(q => q.DriverId);

            var updatedCount = 0;
            var addedCount = 0;

            foreach (var qualifying in qualifyingList)
            {
                if (existingQualifyings.TryGetValue(qualifying.DriverId, out var existing))
                {
                    // Update existing (tracked entity, no need to call Update())
                    existing.Number = qualifying.Number;
                    existing.Position = qualifying.Position;
                    existing.ConstructorId = qualifying.ConstructorId;
                    existing.Q1 = qualifying.Q1;
                    existing.Q2 = qualifying.Q2;
                    existing.Q3 = qualifying.Q3;
                    updatedCount++;
                }
                else
                {
                    // Add new
                    qualifying.Season = season;
                    qualifying.Round = round;
                    await _context.Qualifyings.AddAsync(qualifying);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Batch saved qualifying for season {Season}, round {Round}: {Added} added, {Updated} updated",
                season, round, addedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch saving qualifying for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    public async Task<IEnumerable<Qualifying>> GetBySeasonAsync(string season)
    {
        _logger.LogDebug("Fetching qualifying results for season {Season}", season);
        var qualifyings = await _context.Qualifyings
            .AsNoTracking()
            .Where(q => q.Season == season)
            .ToListAsync();
        
        return qualifyings
            .OrderBy(q => int.Parse(q.Round))
            .ThenBy(q => int.Parse(q.Position));
    }

    public async Task<IEnumerable<Qualifying>> GetByRaceAsync(string season, string round)
    {
        _logger.LogDebug("Fetching qualifying results for season {Season}, round {Round}", season, round);
        var qualifyings = await _context.Qualifyings
            .AsNoTracking()
            .Where(q => q.Season == season && q.Round == round)
            .ToListAsync();
        
        return qualifyings.OrderBy(q => int.Parse(q.Position));
    }

    public async Task<Qualifying?> GetByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogDebug("Fetching qualifying for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        return await _context.Qualifyings
            .AsNoTracking()
            .FirstOrDefaultAsync(q => 
                q.Season == season && 
                q.Round == round && 
                q.DriverId == driverId);
    }

    public async Task<IEnumerable<Qualifying>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all qualifying results");
        var qualifyings = await _context.Qualifyings.AsNoTracking().ToListAsync();
        
        return qualifyings
            .OrderBy(q => q.Season)
            .ThenBy(q => int.Parse(q.Round))
            .ThenBy(q => int.Parse(q.Position));
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all qualifying results from database");
        _context.Qualifyings.RemoveRange(_context.Qualifyings);
        await _context.SaveChangesAsync();
    }
}
