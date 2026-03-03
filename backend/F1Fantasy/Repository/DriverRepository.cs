using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class DriverRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<DriverRepository> _logger;

    public DriverRepository(F1FantasyDbContext context, ILogger<DriverRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(Driver driver)
    {
        try
        {
            var existing = await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driver.DriverId);
            
            if (existing != null)
            {
                _logger.LogDebug("Updating existing driver: {DriverId}", driver.DriverId);
                
                // Update primitive properties
                existing.PermanentNumber = driver.PermanentNumber;
                existing.Code = driver.Code;
                existing.GivenName = driver.GivenName;
                existing.FamilyName = driver.FamilyName;
                existing.DateOfBirth = driver.DateOfBirth;
                existing.Nationality = driver.Nationality;
                existing.Url = driver.Url;
                
                // Merge ActiveSeasons - add any new seasons that aren't already present
                foreach (var season in driver.ActiveSeasons)
                {
                    if (!existing.ActiveSeasons.Contains(season))
                    {
                        existing.ActiveSeasons.Add(season);
                    }
                }
            }
            else
            {
                _logger.LogDebug("Adding new driver: {DriverId}", driver.DriverId);
                await _context.Drivers.AddAsync(driver);
            }
            
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving driver: {DriverId}", driver.DriverId);
            throw;
        }
    }
    
    public async Task AddOrUpdateBatchAsync(IEnumerable<Driver> drivers)
    {
        var driverList = drivers.ToList();
        if (!driverList.Any())
        {
            return;
        }
        
        try
        {
            var driverIds = driverList.Select(d => d.DriverId).ToList();
            
            // Single query to get all existing drivers
            var existingDrivers = await _context.Drivers
                .Where(d => driverIds.Contains(d.DriverId))
                .ToDictionaryAsync(d => d.DriverId);
            
            var addedCount = 0;
            var updatedCount = 0;
            
            foreach (var driver in driverList)
            {
                if (existingDrivers.TryGetValue(driver.DriverId, out var existing))
                {
                    // Update existing driver
                    existing.PermanentNumber = driver.PermanentNumber;
                    existing.Code = driver.Code;
                    existing.GivenName = driver.GivenName;
                    existing.FamilyName = driver.FamilyName;
                    existing.DateOfBirth = driver.DateOfBirth;
                    existing.Nationality = driver.Nationality;
                    existing.Url = driver.Url;
                    
                    // Merge ActiveSeasons
                    foreach (var season in driver.ActiveSeasons)
                    {
                        if (!existing.ActiveSeasons.Contains(season))
                        {
                            existing.ActiveSeasons.Add(season);
                        }
                    }
                    updatedCount++;
                }
                else
                {
                    // Add new driver
                    await _context.Drivers.AddAsync(driver);
                    addedCount++;
                }
            }
            
            // Single SaveChanges call for all changes
            await _context.SaveChangesAsync();
            _logger.LogInformation("Batch processed {Total} drivers: {Added} added, {Updated} updated", 
                driverList.Count, addedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch saving {Count} drivers", driverList.Count);
            throw;
        }
    }

    public async Task<Driver?> GetByDriverIdAsync(string driverId)
    {
        try
        {
            return await _context.Drivers.FirstOrDefaultAsync(d => d.DriverId == driverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving driver: {DriverId}", driverId);
            throw;
        }
    }

    public async Task<IEnumerable<Driver>> GetAllAsync()
    {
        try
        {
            var drivers = await _context.Drivers.OrderBy(d => d.FamilyName).ToListAsync();
            _logger.LogDebug("Retrieved {Count} drivers from database", drivers.Count);
            return drivers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all drivers from database");
            throw;
        }
    }

    public async Task<IEnumerable<Driver>> GetActiveDriversAsync(string? season = null)
    {
        try
        {
            // Use current year if season not specified
            season ??= DateTime.UtcNow.Year.ToString();
            
            // Get drivers that have the season in their ActiveSeasons list
            var activeDrivers = await _context.Drivers
                .Where(d => d.ActiveSeasons.Contains(season))
                .OrderBy(d => d.FamilyName)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {Count} active drivers for season {Season}", activeDrivers.Count, season);
            return activeDrivers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active drivers for season {Season}", season);
            throw;
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            _context.Drivers.RemoveRange(_context.Drivers);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Cleared all drivers from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing drivers from database");
            throw;
        }
    }
}
