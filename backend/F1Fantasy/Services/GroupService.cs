using F1Fantasy.Models;
using F1Fantasy.Repository;
using F1Fantasy.Validation;
using System.Diagnostics;

namespace F1Fantasy.Services;

public class GroupService
{
    private readonly GroupRepository _groupRepository;
    private readonly ILogger<GroupService> _logger;

    public GroupService(GroupRepository groupRepository, ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task<Group> CreateGroupAsync(string name, string adminUserId, string lockMode)
    {
        // Validate inputs
        ValidationExtensions.ValidateGroupName(name);

        // Generate unique invite code
        var inviteCode = GenerateInviteCode();
        
        var group = new Group
        {
            Name = name,
            AdminUserId = adminUserId,
            LockMode = lockMode,
            InviteCode = inviteCode,
            CreatedAt = DateTime.UtcNow,
            PredictionsLocked = false
        };

        var createdGroup = await _groupRepository.CreateAsync(group);

        // Auto-add admin as first member
        await _groupRepository.AddMemberAsync(new GroupMember
        {
            GroupId = createdGroup.Id,
            UserId = adminUserId,
            JoinedAt = DateTime.UtcNow
        });

        return createdGroup;
    }

    public async Task<Group?> GetGroupByIdAsync(int id)
    {
        return await _groupRepository.GetByIdAsync(id);
    }

    public async Task<Group?> GetGroupByInviteCodeAsync(string inviteCode)
    {
        return await _groupRepository.GetByInviteCodeAsync(inviteCode);
    }

    public async Task<List<Group>> GetUserGroupsAsync(string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[GroupService.GetUserGroupsAsync] Start - UserId: {UserId}", userId);
        
        var result = await _groupRepository.GetGroupsByUserIdAsync(userId);
        
        stopwatch.Stop();
        _logger.LogInformation("[GroupService.GetUserGroupsAsync] Complete - GroupCount: {Count}, Elapsed: {Elapsed}ms", result.Count, stopwatch.ElapsedMilliseconds);
        return result;
    }

    public async Task<GroupMember> JoinGroupAsync(int groupId, string userId)
    {
        // Check if user is already a member
        if (await _groupRepository.IsUserMemberAsync(groupId, userId))
        {
            throw new InvalidOperationException("User is already a member of this group");
        }

        return await _groupRepository.AddMemberAsync(new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
    }

    public async Task LeaveGroupAsync(int groupId, string userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        if (group.AdminUserId == userId)
        {
            throw new InvalidOperationException("Admin cannot leave the group. Transfer admin rights or delete the group.");
        }

        await _groupRepository.RemoveMemberAsync(groupId, userId);
    }

    public async Task RenameGroupAsync(int groupId, string userId, string newName)
    {
        // Validate input first
        ValidationExtensions.ValidateGroupName(newName);

        if (!await _groupRepository.IsUserAdminAsync(groupId, userId))
        {
            throw new UnauthorizedAccessException("Only admin can rename the group");
        }

        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        group.Name = newName;
        await _groupRepository.UpdateAsync(group);
    }

    public async Task RemoveMemberAsync(int groupId, string adminUserId, string targetUserId)
    {
        if (!await _groupRepository.IsUserAdminAsync(groupId, adminUserId))
        {
            throw new UnauthorizedAccessException("Only admin can remove members from the group");
        }

        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        if (group.AdminUserId == targetUserId)
        {
            throw new InvalidOperationException("Cannot remove the admin from the group");
        }

        if (!await _groupRepository.IsUserMemberAsync(groupId, targetUserId))
        {
            throw new InvalidOperationException("User is not a member of this group");
        }

        await _groupRepository.RemoveMemberAsync(groupId, targetUserId);
    }

    public async Task DeleteGroupAsync(int groupId, string userId)
    {
        if (!await _groupRepository.IsUserAdminAsync(groupId, userId))
        {
            throw new UnauthorizedAccessException("Only admin can delete the group");
        }

        await _groupRepository.DeleteAsync(groupId);
    }

    public async Task LockPredictionsAsync(int groupId, string userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        // For admin mode, only admin can lock
        if (group.LockMode == "admin" && group.AdminUserId != userId)
        {
            throw new UnauthorizedAccessException("Only admin can lock predictions in admin mode");
        }

        await _groupRepository.SetPredictionsLockedAsync(groupId, true);
    }

    public async Task UnlockPredictionsAsync(int groupId, string userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        // For admin mode, only admin can unlock
        if (group.LockMode == "admin" && group.AdminUserId != userId)
        {
            throw new UnauthorizedAccessException("Only admin can unlock predictions in admin mode");
        }

        // System mode doesn't allow manual unlock
        if (group.LockMode == "system")
        {
            throw new InvalidOperationException("Cannot manually unlock in system mode");
        }

        await _groupRepository.SetPredictionsLockedAsync(groupId, false);
    }

    private string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed ambiguous chars
        // Use Random.Shared for thread-safety and better randomness
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }
}
