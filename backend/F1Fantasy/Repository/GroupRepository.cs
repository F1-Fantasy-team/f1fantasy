using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace F1Fantasy.Repository;

public class GroupRepository
{
    private readonly F1FantasyDbContext _context;
    private readonly ILogger<GroupRepository> _logger;

    public GroupRepository(F1FantasyDbContext context, ILogger<GroupRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Group?> GetByIdAsync(int id)
    {
        return await _context.Groups
            .Include(g => g.Members)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Group?> GetByInviteCodeAsync(string inviteCode)
    {
        return await _context.Groups
            .Include(g => g.Members)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.InviteCode == inviteCode);
    }

    public async Task<List<Group>> GetAllGroupsAsync()
    {
        return await _context.Groups
            .Include(g => g.Members)
            .ToListAsync();
    }

    public async Task<List<Group>> GetGroupsByUserIdAsync(string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GroupRepository.GetGroupsByUserIdAsync] Start - UserId: {UserId}", userId);
        
        // Optimized: Get distinct group IDs first, then fetch groups
        // This avoids the expensive Distinct() operation on full entities
        // and prevents loading unnecessary members data in the initial query
        var groupIds = await _context.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.GroupId)
            .Distinct()
            .ToListAsync();
        
        // Now fetch the groups with their members using the IDs
        // This is more efficient than the previous approach
        var result = await _context.Groups
            .Where(g => groupIds.Contains(g.Id))
            .Include(g => g.Members)
            .OrderBy(g => g.Id)
            .ToListAsync();
        
        stopwatch.Stop();
        _logger.LogInformation("[GroupRepository.GetGroupsByUserIdAsync] Complete - GroupCount: {Count}, MemberCount: {MemberCount}, Elapsed: {Elapsed}ms", 
            result.Count, 
            result.Sum(g => g.Members.Count), 
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    public async Task<Group> CreateAsync(Group group)
    {
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<Group> UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task DeleteAsync(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);
        
        if (group != null)
        {
            // Explicitly remove all members first to ensure cascade works
            _context.GroupMembers.RemoveRange(group.Members);
            
            // Then remove the group
            _context.Groups.Remove(group);
            
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsUserMemberAsync(int groupId, string userId)
    {
        return await _context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<bool> IsUserAdminAsync(int groupId, string userId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        return group?.AdminUserId == userId;
    }

    public async Task<GroupMember> AddMemberAsync(GroupMember member)
    {
        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task RemoveMemberAsync(int groupId, string userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        
        if (member != null)
        {
            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<GroupMember>> GetMembersAsync(int groupId)
    {
        return await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .ToListAsync();
    }

    public async Task SetPredictionsLockedAsync(int groupId, bool isLocked)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group != null)
        {
            group.PredictionsLocked = isLocked;
            group.LockedAt = isLocked ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();
        }
    }
}
