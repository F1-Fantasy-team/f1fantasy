using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class DriverRepository
{
    private readonly F1FantasyDbContext _context;

    public DriverRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task AddOrUpdateAsync(Driver driver)
    {
        var existing = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driver.DriverId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(driver);
        }
        else
        {
            await _context.Drivers.AddAsync(driver);
        }
        
        await _context.SaveChangesAsync();
    }

    public async Task<Driver?> GetByDriverIdAsync(string driverId)
    {
        return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
    }

    public async Task<IEnumerable<Driver>> GetAllAsync()
    {
        return await _context.Drivers.OrderBy(d => d.FamilyName).ToListAsync();
    }

    public async Task ClearAsync()
    {
        _context.Drivers.RemoveRange(_context.Drivers);
        await _context.SaveChangesAsync();
    }
}
