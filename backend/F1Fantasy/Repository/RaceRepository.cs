using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class RaceRepository
{
    private readonly F1FantasyDbContext _context;

    public RaceRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateAsync(Race race)
    {
        var existing = await _context.Races
            .FirstOrDefaultAsync(r => r.Season == race.Season && r.Round == race.Round);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(race);
            existing.Circuit = race.Circuit;
            existing.FirstPractice = race.FirstPractice;
            existing.SecondPractice = race.SecondPractice;
            existing.ThirdPractice = race.ThirdPractice;
            existing.Qualifying = race.Qualifying;
            existing.Sprint = race.Sprint;
            existing.SprintQualifying = race.SprintQualifying;
        }
        else
        {
            await _context.Races.AddAsync(race);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<Race?> GetByRoundAsync(string season, string round)
    {
        return await _context.Races
            .FirstOrDefaultAsync(r => r.Season == season && r.Round == round);
    }

    public async Task<IEnumerable<Race>> GetAllAsync()
    {
        return await _context.Races.ToListAsync();
    }

    public async Task<IEnumerable<Race>> GetBySeasonAsync(string season)
    {
        return await _context.Races
            .Where(r => r.Season == season)
            .ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.Races.RemoveRange(_context.Races);
        await _context.SaveChangesAsync();
    }
}