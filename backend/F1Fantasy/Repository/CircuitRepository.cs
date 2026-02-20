using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class CircuitRepository
{
    private readonly F1FantasyDbContext _context;

    public CircuitRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateAsync(Circuit circuit)
    {
        var existing = await _context.Circuits.FirstOrDefaultAsync(c => c.CircuitId == circuit.CircuitId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(circuit);
            existing.Location = circuit.Location;
        }
        else
        {
            await _context.Circuits.AddAsync(circuit);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<Circuit?> GetByCircuitIdAsync(string circuitId)
    {
        return await _context.Circuits.FirstOrDefaultAsync(c => c.CircuitId == circuitId);
    }

    public async Task<IEnumerable<Circuit>> GetAllAsync()
    {
        return await _context.Circuits.OrderBy(c => c.CircuitName).ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.Circuits.RemoveRange(_context.Circuits);
        await _context.SaveChangesAsync();
    }
}
