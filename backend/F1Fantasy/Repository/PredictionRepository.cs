using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class PredictionRepository
{
    private readonly IDbContextFactory<F1FantasyDbContext> _contextFactory;

    public PredictionRepository(IDbContextFactory<F1FantasyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Constructor Championship
    public async Task<ConstructorChampionshipPrediction?> GetConstructorChampionshipAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConstructorChampionshipPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<ConstructorChampionshipPrediction>> GetAllConstructorChampionshipsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ConstructorChampionshipPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<ConstructorChampionshipPrediction> UpsertConstructorChampionshipAsync(ConstructorChampionshipPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ConstructorChampionshipPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.RankedConstructorIds = prediction.RankedConstructorIds;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.ConstructorChampionshipPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Driver Championship
    public async Task<DriverChampionshipPrediction?> GetDriverChampionshipAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DriverChampionshipPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DriverChampionshipPrediction>> GetAllDriverChampionshipsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DriverChampionshipPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DriverChampionshipPrediction> UpsertDriverChampionshipAsync(DriverChampionshipPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.DriverChampionshipPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.RankedDriverIds = prediction.RankedDriverIds;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.DriverChampionshipPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Driver Draft
    public async Task<DriverDraftPrediction?> GetDriverDraftAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DriverDraftPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DriverDraftPrediction>> GetAllDriverDraftsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DriverDraftPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DriverDraftPrediction> UpsertDriverDraftAsync(DriverDraftPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.DriverDraftPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.DriverDraftPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Destructors
    public async Task<DestructorPrediction?> GetDestructorAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestructorPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DestructorPrediction>> GetAllDestructorsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.DestructorPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DestructorPrediction> UpsertDestructorAsync(DestructorPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.DestructorPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.DestructorPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Mr Saturday
    public async Task<MrSaturdayPrediction?> GetMrSaturdayAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MrSaturdayPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<MrSaturdayPrediction>> GetAllMrSaturdaysAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MrSaturdayPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<MrSaturdayPrediction> UpsertMrSaturdayAsync(MrSaturdayPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.MrSaturdayPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.MrSaturdayPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Zero Pointers
    public async Task<ZeroPointerPrediction?> GetZeroPointerAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ZeroPointerPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<ZeroPointerPrediction>> GetAllZeroPointersAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ZeroPointerPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<ZeroPointerPrediction> UpsertZeroPointerAsync(ZeroPointerPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.ZeroPointerPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.DriverIds = prediction.DriverIds;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.ZeroPointerPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Wildcard
    public async Task<WildcardPrediction?> GetWildcardAsync(int groupId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WildcardPredictions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<WildcardPrediction>> GetAllWildcardsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.WildcardPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.UserId)
            .ToListAsync();
    }

    public async Task<WildcardPrediction> UpsertWildcardAsync(WildcardPrediction prediction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.WildcardPredictions
            .FirstOrDefaultAsync(p => p.GroupId == prediction.GroupId && p.UserId == prediction.UserId);
        
        if (existing != null)
        {
            existing.Statement = prediction.Statement;
            existing.PointsPotential = prediction.PointsPotential;
            existing.Fullfilled = prediction.Fullfilled;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            context.WildcardPredictions.Add(prediction);
        }
        
        await context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Get all predictions for a user (for tie-breaking in standings)
    // Each Get method creates its own DbContext, so concurrent execution is safe
    public async Task<UserPredictions> GetAllPredictionsAsync(int groupId, string userId)
    {
        return new UserPredictions
        {
            DriverChampionship = await GetDriverChampionshipAsync(groupId, userId),
            ConstructorChampionship = await GetConstructorChampionshipAsync(groupId, userId),
            DriverDraft = await GetDriverDraftAsync(groupId, userId),
            Destructor = await GetDestructorAsync(groupId, userId),
            MrSaturday = await GetMrSaturdayAsync(groupId, userId),
            ZeroPointer = await GetZeroPointerAsync(groupId, userId),
            Wildcard = await GetWildcardAsync(groupId, userId)
        };
    }
}

// Helper class for bundling all prediction types
public class UserPredictions
{
    public DriverChampionshipPrediction? DriverChampionship { get; set; }
    public ConstructorChampionshipPrediction? ConstructorChampionship { get; set; }
    public DriverDraftPrediction? DriverDraft { get; set; }
    public DestructorPrediction? Destructor { get; set; }
    public MrSaturdayPrediction? MrSaturday { get; set; }
    public ZeroPointerPrediction? ZeroPointer { get; set; }
    public WildcardPrediction? Wildcard { get; set; }
}
