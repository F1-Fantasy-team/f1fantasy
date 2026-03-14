using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Repository;

public class StandingRepository
{
    private readonly F1FantasyDbContext _context;

    public StandingRepository(F1FantasyDbContext context)
    {
        _context = context;
    }

    public async Task<Standing?> GetByUserAndGroupAsync(int groupId, string userId)
    {
        return await _context.Standings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.GroupId == groupId && s.UserId == userId);
    }

    public async Task<List<Standing>> GetStandingsByGroupAsync(int groupId)
    {
        return await _context.Standings
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.Rank)
            .ToListAsync();
    }

    public async Task<Standing> UpsertAsync(Standing standing)
    {
        // Query with tracking enabled for update operation
        var existing = await _context.Standings
            .FirstOrDefaultAsync(s => s.GroupId == standing.GroupId && s.UserId == standing.UserId);
        
        if (existing != null)
        {
            existing.TotalScore = standing.TotalScore;
            existing.Rank = standing.Rank;
            existing.CategoryScoresJson = standing.CategoryScoresJson;
            existing.UpdatedAt = DateTime.UtcNow;
            // No need to call Update - entity is already tracked
        }
        else
        {
            standing.UpdatedAt = DateTime.UtcNow;
            _context.Standings.Add(standing);
        }
        
        await _context.SaveChangesAsync();
        return existing ?? standing;
    }

    public async Task UpsertManyAsync(List<Standing> standings)
    {
        if (!standings.Any()) return;

        var groupId = standings.First().GroupId;
        var userIds = standings.Select(s => s.UserId).ToList();

        // Single query to get all existing standings for these users in this group
        var existingStandings = await _context.Standings
            .Where(s => s.GroupId == groupId && userIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId);

        foreach (var standing in standings)
        {
            if (existingStandings.TryGetValue(standing.UserId, out var existing))
            {
                // Update existing - entity is already tracked
                existing.TotalScore = standing.TotalScore;
                existing.Rank = standing.Rank;
                existing.CategoryScoresJson = standing.CategoryScoresJson;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new
                standing.UpdatedAt = DateTime.UtcNow;
                _context.Standings.Add(standing);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteByGroupAsync(int groupId)
    {
        var standings = await _context.Standings
            .Where(s => s.GroupId == groupId)
            .ToListAsync();
        
        _context.Standings.RemoveRange(standings);
        await _context.SaveChangesAsync();
    }
}
