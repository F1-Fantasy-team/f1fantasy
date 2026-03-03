using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class ConstructorRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<ConstructorRepository> _logger;

    public ConstructorRepository(F1FantasyDbContext context, ILogger<ConstructorRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddOrUpdateAsync(Constructor constructor)
    {
        var existing = await _context.Constructors.FirstOrDefaultAsync(c => c.ConstructorId == constructor.ConstructorId);
        
        if (existing != null)
        {
            // Update primitive properties
            existing.Name = constructor.Name;
            existing.Url = constructor.Url;
            existing.Nationality = constructor.Nationality;
            
            // Merge ActiveSeasons - add any new seasons that aren't already present
            foreach (var season in constructor.ActiveSeasons)
            {
                if (!existing.ActiveSeasons.Contains(season))
                {
                    existing.ActiveSeasons.Add(season);
                }
            }
        }
        else
        {
            await _context.Constructors.AddAsync(constructor);
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task AddOrUpdateBatchAsync(IEnumerable<Constructor> constructors)
    {
        var constructorList = constructors.ToList();
        if (!constructorList.Any())
        {
            return;
        }
        
        try
        {
            var constructorIds = constructorList.Select(c => c.ConstructorId).ToList();
            
            // Single query to get all existing constructors
            var existingConstructors = await _context.Constructors
                .Where(c => constructorIds.Contains(c.ConstructorId))
                .ToDictionaryAsync(c => c.ConstructorId);
            
            var addedCount = 0;
            var updatedCount = 0;
            
            foreach (var constructor in constructorList)
            {
                if (existingConstructors.TryGetValue(constructor.ConstructorId, out var existing))
                {
                    // Log incoming data for diagnosis
                    _logger.LogDebug("[AddOrUpdateBatchAsync] Constructor {ConstructorId} incoming ActiveSeasons: [{Seasons}]",
                        constructor.ConstructorId, string.Join(", ", constructor.ActiveSeasons));
                    
                    // Update existing constructor
                    existing.Name = constructor.Name;
                    existing.Url = constructor.Url;
                    existing.Nationality = constructor.Nationality;
                    
                    // Update ActiveSeasons - explicitly mark as modified for EF Core to track PostgreSQL array
                    existing.ActiveSeasons = constructor.ActiveSeasons.ToList();
                    _context.Entry(existing).Property(e => e.ActiveSeasons).IsModified = true;
                    
                    _logger.LogDebug("[AddOrUpdateBatchAsync] Constructor {ConstructorId} after assignment: [{Seasons}], IsModified: {IsModified}",
                        constructor.ConstructorId, string.Join(", ", existing.ActiveSeasons),
                        _context.Entry(existing).Property(e => e.ActiveSeasons).IsModified);
                    
                    updatedCount++;
                }
                else
                {
                    // Add new constructor
                    await _context.Constructors.AddAsync(constructor);
                    addedCount++;
                }
            }
            
            // Single SaveChanges call for all changes
            await _context.SaveChangesAsync();
            _logger.LogInformation("Batch processed {Total} constructors: {Added} added, {Updated} updated", 
                constructorList.Count, addedCount, updatedCount);
            
            // Verify what was actually written to database
            var verifyConstructor = await _context.Constructors
                .AsNoTracking()
                .FirstOrDefaultAsync(c => constructorIds.Contains(c.ConstructorId));
            if (verifyConstructor != null)
            {
                _logger.LogWarning("[AddOrUpdateBatchAsync] POST-SAVE verification: Constructor {ConstructorId} in DB has ActiveSeasons: [{Seasons}]",
                    verifyConstructor.ConstructorId, string.Join(", ", verifyConstructor.ActiveSeasons));
            }
            
            // Log a sample to verify ActiveSeasons
            var sampleConstructor = constructorList.FirstOrDefault();
            if (sampleConstructor != null)
            {
                _logger.LogInformation("Sample constructor {ConstructorId} has ActiveSeasons: [{Seasons}]", 
                    sampleConstructor.ConstructorId, string.Join(", ", sampleConstructor.ActiveSeasons));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch saving {Count} constructors", constructorList.Count);
            throw;
        }
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
        
        _logger.LogInformation("[GetActiveConstructorsAsync] Querying for active constructors in season {Season}", season);
        
        // Get constructors that have the season in their ActiveSeasons list
        var activeConstructors = await _context.Constructors
            .AsNoTracking()
            .Where(c => c.ActiveSeasons.Contains(season))
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        _logger.LogInformation("[GetActiveConstructorsAsync] Found {Count} active constructors for season {Season}", activeConstructors.Count, season);
        
        // Debug: Check total constructors in DB
        var totalConstructors = await _context.Constructors.AsNoTracking().CountAsync();
        _logger.LogInformation("[GetActiveConstructorsAsync] Total constructors in database: {Total}", totalConstructors);
        
        // Debug: Sample a constructor to see their ActiveSeasons
        var sampleConstructor = await _context.Constructors.AsNoTracking().FirstOrDefaultAsync();
        if (sampleConstructor != null)
        {
            _logger.LogInformation("[GetActiveConstructorsAsync] Sample constructor {ConstructorId} has ActiveSeasons: [{Seasons}]", 
                sampleConstructor.ConstructorId, string.Join(", ", sampleConstructor.ActiveSeasons));
        }
        
        return activeConstructors;
    }

    public async Task ClearAsync()
    {
        _context.Constructors.RemoveRange(_context.Constructors);
        await _context.SaveChangesAsync();
    }
}
