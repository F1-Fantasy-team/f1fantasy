using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class DriverStandingRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<DriverStandingRepository> _logger;

    public DriverStandingRepository(F1FantasyDbContext context, ILogger<DriverStandingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(DriverStanding standing)
    {
        try
        {
            var existing = await _context.DriverStandings
                .FirstOrDefaultAsync(s => 
                    s.Season == standing.Season && 
                    s.DriverId == standing.DriverId);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing driver standing: Season {Season}, Driver {DriverId}, Position {Position}", 
                    standing.Season, standing.DriverId, standing.Position);
                
                existing.Round = standing.Round;
                existing.Position = standing.Position;
                existing.PositionText = standing.PositionText;
                existing.Points = standing.Points;
                existing.Wins = standing.Wins;
                existing.ConstructorId = standing.ConstructorId;
            }
            else
            {
                _logger.LogDebug("Adding new driver standing: Season {Season}, Driver {DriverId}, Position {Position}", 
                    standing.Season, standing.DriverId, standing.Position);
                
                await _context.DriverStandings.AddAsync(standing);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving driver standing: Season {Season}, Driver {DriverId}", 
                standing.Season, standing.DriverId);
            throw;
        }
    }

    public async Task<IEnumerable<DriverStanding>> GetBySeasonAndRoundAsync(string season, string round)
    {
        _logger.LogDebug("Fetching driver standings for season {Season}, round {Round}", season, round);
        var standings = await _context.DriverStandings
            .AsNoTracking()
            .Where(s => s.Season == season && s.Round == round)
            .ToListAsync();
        
        // Filter out any standings with empty Position before parsing
        return standings
            .Where(s => !string.IsNullOrEmpty(s.Position))
            .OrderBy(s => int.Parse(s.Position));
    }

    public async Task<IEnumerable<DriverStanding>> GetBySeasonAsync(string season)
    {
        _logger.LogDebug("Fetching latest driver standings for season {Season}", season);
        
        // Get the maximum round for this season
        var maxRound = await _context.DriverStandings
            .AsNoTracking()
            .Where(s => s.Season == season)
            .Select(s => s.Round)
            .Distinct()
            .ToListAsync();
        
        if (!maxRound.Any())
        {
            return Enumerable.Empty<DriverStanding>();
        }
        
        var latestRound = maxRound.Max(r => int.Parse(r)).ToString();
        
        return await GetBySeasonAndRoundAsync(season, latestRound);
    }

    public async Task<DriverStanding?> GetByDriverAsync(string season, string round, string driverId)
    {
        _logger.LogDebug("Fetching driver standing for season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        return await _context.DriverStandings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => 
                s.Season == season && 
                s.Round == round && 
                s.DriverId == driverId);
    }

    public async Task<IEnumerable<DriverStanding>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all driver standings");
        var standings = await _context.DriverStandings.AsNoTracking().ToListAsync();
        
        // Filter out any standings with empty Position or Round before parsing
        return standings
            .Where(s => !string.IsNullOrEmpty(s.Round) && !string.IsNullOrEmpty(s.Position))
            .OrderBy(s => s.Season)
            .ThenBy(s => int.Parse(s.Round))
            .ThenBy(s => int.Parse(s.Position));
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all driver standings from database");
        _context.DriverStandings.RemoveRange(_context.DriverStandings);
        await _context.SaveChangesAsync();
    }
}
