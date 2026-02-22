using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class ConstructorRepository
{
    private readonly F1FantasyDbContext _context;

    public ConstructorRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateAsync(Constructor constructor)
    {
        var existing = await _context.Constructors.FirstOrDefaultAsync(c => c.ConstructorId == constructor.ConstructorId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(constructor);
        }
        else
        {
            await _context.Constructors.AddAsync(constructor);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<Constructor?> GetByConstructorIdAsync(string constructorId)
    {
        return await _context.Constructors.FirstOrDefaultAsync(c => c.ConstructorId == constructorId);
    }

    public async Task<IEnumerable<Constructor>> GetAllAsync()
    {
        return await _context.Constructors.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IEnumerable<Constructor>> GetActiveConstructorsAsync(string? season = null)
    {
        // Use current year if season not specified
        season ??= DateTime.UtcNow.Year.ToString();
        
        // Get constructors that have the season in their ActiveSeasons list
        return await _context.Constructors
            .Where(c => c.ActiveSeasons.Contains(season))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.Constructors.RemoveRange(_context.Constructors);
        await _context.SaveChangesAsync();
    }
}
