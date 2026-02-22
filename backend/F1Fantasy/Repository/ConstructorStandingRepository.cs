using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class ConstructorStandingRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<ConstructorStandingRepository> _logger;

    public ConstructorStandingRepository(F1FantasyDbContext context, ILogger<ConstructorStandingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(ConstructorStanding standing)
    {
        var existingStanding = await _context.ConstructorStandings
            .FirstOrDefaultAsync(cs => cs.Season == standing.Season && cs.ConstructorId == standing.ConstructorId);

        if (existingStanding != null)
        {
            // Update only the mutable fields
            existingStanding.Round = standing.Round;
            existingStanding.Position = standing.Position;
            existingStanding.PositionText = standing.PositionText;
            existingStanding.Points = standing.Points;
            existingStanding.Wins = standing.Wins;
            
            _logger.LogDebug("Updating constructor standing: {Season} - {ConstructorId}", standing.Season, standing.ConstructorId);
        }
        else
        {
            await _context.ConstructorStandings.AddAsync(standing);
            _logger.LogDebug("Adding new constructor standing: {Season} - {ConstructorId}", standing.Season, standing.ConstructorId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<ConstructorStanding>> GetBySeasonAndRoundAsync(string season, string round)
    {
        return await _context.ConstructorStandings
            .Where(cs => cs.Season == season && cs.Round == round)
            .OrderBy(cs => int.Parse(cs.Position))
            .ToListAsync();
    }

    public async Task<List<ConstructorStanding>> GetBySeasonAsync(string season)
    {
        // Get the latest round for this season
        var maxRound = await _context.ConstructorStandings
            .Where(cs => cs.Season == season)
            .MaxAsync(cs => (int?)int.Parse(cs.Round));

        if (maxRound == null)
            return new List<ConstructorStanding>();

        return await _context.ConstructorStandings
            .Where(cs => cs.Season == season && cs.Round == maxRound.ToString())
            .OrderBy(cs => int.Parse(cs.Position))
            .ToListAsync();
    }

    public async Task<ConstructorStanding?> GetByConstructorAsync(string season, string round, string constructorId)
    {
        return await _context.ConstructorStandings
            .FirstOrDefaultAsync(cs => cs.Season == season && cs.Round == round && cs.ConstructorId == constructorId);
    }

    public async Task<List<ConstructorStanding>> GetAllAsync()
    {
        var standings = await _context.ConstructorStandings.ToListAsync();
        
        return standings
            .OrderBy(cs => cs.Season)
            .ThenBy(cs => int.Parse(cs.Round))
            .ThenBy(cs => int.Parse(cs.Position))
            .ToList();
    }

    public async Task ClearAsync()
    {
        _logger.LogWarning("Clearing all constructor standings from database");
        _context.ConstructorStandings.RemoveRange(_context.ConstructorStandings);
        await _context.SaveChangesAsync();
    }
}
