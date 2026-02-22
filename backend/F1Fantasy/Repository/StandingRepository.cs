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
            .FirstOrDefaultAsync(s => s.GroupId == groupId && s.UserId == userId);
    }

    public async Task<List<Standing>> GetStandingsByGroupAsync(int groupId)
    {
        return await _context.Standings
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.Rank)
            .ToListAsync();
    }

    public async Task<Standing> UpsertAsync(Standing standing)
    {
        var existing = await GetByUserAndGroupAsync(standing.GroupId, standing.UserId);
        
        if (existing != null)
        {
            existing.TotalScore = standing.TotalScore;
            existing.Rank = standing.Rank;
            existing.CategoryScoresJson = standing.CategoryScoresJson;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.Standings.Update(existing);
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
        foreach (var standing in standings)
        {
            await UpsertAsync(standing);
        }
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
