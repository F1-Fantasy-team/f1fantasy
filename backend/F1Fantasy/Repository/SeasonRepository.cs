using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class SeasonRepository
{
    private readonly F1FantasyDbContext _context;

    public SeasonRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateAsync(Season season)
    {
        var existing = await _context.Seasons.FirstOrDefaultAsync(s => s.Year == season.Year);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(season);
        }
        else
        {
            await _context.Seasons.AddAsync(season);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<Season?> GetByYearAsync(string year)
    {
        return await _context.Seasons.AsNoTracking().FirstOrDefaultAsync(s => s.Year == year);
    }

    public async Task<IEnumerable<Season>> GetAllAsync()
    {
        return await _context.Seasons.AsNoTracking().OrderBy(s => s.Year).ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.Seasons.RemoveRange(_context.Seasons);
        await _context.SaveChangesAsync();
    }
}
