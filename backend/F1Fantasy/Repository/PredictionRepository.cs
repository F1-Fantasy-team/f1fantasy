using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class PredictionRepository
{
    private readonly F1FantasyDbContext _context;

    public PredictionRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    // Constructor Championship
    public async Task<ConstructorChampionshipPrediction?> GetConstructorChampionshipAsync(int groupId, string userId)
    {
        return await _context.ConstructorChampionshipPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<ConstructorChampionshipPrediction>> GetAllConstructorChampionshipsAsync(int groupId)
    {
        return await _context.ConstructorChampionshipPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<ConstructorChampionshipPrediction> UpsertConstructorChampionshipAsync(ConstructorChampionshipPrediction prediction)
    {
        var existing = await GetConstructorChampionshipAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.RankedConstructorIds = prediction.RankedConstructorIds;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.ConstructorChampionshipPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.ConstructorChampionshipPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Driver Championship
    public async Task<DriverChampionshipPrediction?> GetDriverChampionshipAsync(int groupId, string userId)
    {
        return await _context.DriverChampionshipPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DriverChampionshipPrediction>> GetAllDriverChampionshipsAsync(int groupId)
    {
        return await _context.DriverChampionshipPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DriverChampionshipPrediction> UpsertDriverChampionshipAsync(DriverChampionshipPrediction prediction)
    {
        var existing = await GetDriverChampionshipAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.RankedDriverIds = prediction.RankedDriverIds;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.DriverChampionshipPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.DriverChampionshipPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Driver Draft
    public async Task<DriverDraftPrediction?> GetDriverDraftAsync(int groupId, string userId)
    {
        return await _context.DriverDraftPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DriverDraftPrediction>> GetAllDriverDraftsAsync(int groupId)
    {
        return await _context.DriverDraftPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DriverDraftPrediction> UpsertDriverDraftAsync(DriverDraftPrediction prediction)
    {
        var existing = await GetDriverDraftAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.DriverDraftPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.DriverDraftPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Destructors
    public async Task<DestructorPrediction?> GetDestructorAsync(int groupId, string userId)
    {
        return await _context.DestructorPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<DestructorPrediction>> GetAllDestructorsAsync(int groupId)
    {
        return await _context.DestructorPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<DestructorPrediction> UpsertDestructorAsync(DestructorPrediction prediction)
    {
        var existing = await GetDestructorAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.DestructorPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.DestructorPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Mr Saturday
    public async Task<MrSaturdayPrediction?> GetMrSaturdayAsync(int groupId, string userId)
    {
        return await _context.MrSaturdayPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<MrSaturdayPrediction>> GetAllMrSaturdaysAsync(int groupId)
    {
        return await _context.MrSaturdayPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<MrSaturdayPrediction> UpsertMrSaturdayAsync(MrSaturdayPrediction prediction)
    {
        var existing = await GetMrSaturdayAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.Driver1Id = prediction.Driver1Id;
            existing.Driver2Id = prediction.Driver2Id;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.MrSaturdayPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.MrSaturdayPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Zero Pointers
    public async Task<ZeroPointerPrediction?> GetZeroPointerAsync(int groupId, string userId)
    {
        return await _context.ZeroPointerPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<ZeroPointerPrediction>> GetAllZeroPointersAsync(int groupId)
    {
        return await _context.ZeroPointerPredictions
            .Where(p => p.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<ZeroPointerPrediction> UpsertZeroPointerAsync(ZeroPointerPrediction prediction)
    {
        var existing = await GetZeroPointerAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.DriverIds = prediction.DriverIds;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.ZeroPointerPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.ZeroPointerPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Wildcard
    public async Task<WildcardPrediction?> GetWildcardAsync(int groupId, string userId)
    {
        return await _context.WildcardPredictions
            .FirstOrDefaultAsync(p => p.GroupId == groupId && p.UserId == userId);
    }

    public async Task<List<WildcardPrediction>> GetAllWildcardsAsync(int groupId)
    {
        return await _context.WildcardPredictions
            .AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.UserId)
            .ToListAsync();
    }

    public async Task<WildcardPrediction> UpsertWildcardAsync(WildcardPrediction prediction)
    {
        var existing = await GetWildcardAsync(prediction.GroupId, prediction.UserId);
        
        if (existing != null)
        {
            existing.Statement = prediction.Statement;
            existing.PointsPotential = prediction.PointsPotential;
            existing.Fullfilled = prediction.Fullfilled;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.WildcardPredictions.Update(existing);
        }
        else
        {
            prediction.CreatedAt = DateTime.UtcNow;
            _context.WildcardPredictions.Add(prediction);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? prediction;
    }

    // Get all predictions for a user (for tie-breaking in standings)
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
