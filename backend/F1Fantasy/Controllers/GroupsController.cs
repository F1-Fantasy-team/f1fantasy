using F1Fantasy.Models;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("read")] // Default for GET operations
public class GroupsController : ControllerBase
{
    private readonly GroupService _groupService;
    private readonly ClerkService _clerkService;

    public GroupsController(GroupService groupService, ClerkService clerkService)
    {
        _groupService = groupService;
        _clerkService = clerkService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new UnauthorizedAccessException("User ID not found");
    }

    [HttpPost]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        try
        {
            var userId = GetUserId();
            var group = await _groupService.CreateGroupAsync(request.Name, userId, request.LockMode);
            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        try
        {
            var userId = GetUserId();
            var groups = await _groupService.GetUserGroupsAsync(userId);
            var groupDtos = await EnrichGroupsWithMemberNamesAsync(groups);
            return Ok(groupDtos);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGroup(int id)
    {
        try
        {
            var group = await _groupService.GetGroupByIdAsync(id);
            if (group == null) return NotFound();
            var groupDto = await EnrichGroupWithMemberNamesAsync(group);
            return Ok(groupDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("invite/{inviteCode}")]
    public async Task<IActionResult> GetGroupByInviteCode(string inviteCode)
    {
        try
        {
            var group = await _groupService.GetGroupByInviteCodeAsync(inviteCode);
            if (group == null) return NotFound();
            var groupDto = await EnrichGroupWithMemberNamesAsync(group);
            return Ok(groupDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/join")]
    public async Task<IActionResult> JoinGroup(int id)
    {
        try
        {
            var userId = GetUserId();
            var member = await _groupService.JoinGroupAsync(id, userId);
            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.LeaveGroupAsync(id, userId);
            return Ok(new { message = "Left group successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> RenameGroup(int id, [FromBody] RenameGroupRequest request)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.RenameGroupAsync(id, userId, request.Name);
            return Ok(new { message = "Group renamed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Group not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.DeleteGroupAsync(id, userId);
            return Ok(new { message = "Group deleted successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}/members/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(int id, string targetUserId)
    {
        try
        {
            var adminUserId = GetUserId();
            await _groupService.RemoveMemberAsync(id, adminUserId, targetUserId);
            return Ok(new { message = "Member removed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Group not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> LockPredictions(int id)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.LockPredictionsAsync(id, userId);
            return Ok(new { message = "Predictions locked" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockPredictions(int id)
    {
        try
        {
            var userId = GetUserId();
            await _groupService.UnlockPredictionsAsync(id, userId);
            return Ok(new { message = "Predictions unlocked" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<GroupDto> EnrichGroupWithMemberNamesAsync(Group group)
    {
        var userIds = group.Members.Select(m => m.UserId).ToList();
        var displayNames = await _clerkService.GetUserDisplayNamesAsync(userIds);

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            InviteCode = group.InviteCode,
            LockMode = group.LockMode,
            AdminUserId = group.AdminUserId,
            CreatedAt = group.CreatedAt,
            PredictionsLocked = group.PredictionsLocked,
            LockedAt = group.LockedAt,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                Id = m.Id,
                GroupId = m.GroupId,
                UserId = m.UserId,
                DisplayName = displayNames.GetValueOrDefault(m.UserId, m.UserId),
                IsAdmin = m.UserId == group.AdminUserId,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    private async Task<List<GroupDto>> EnrichGroupsWithMemberNamesAsync(List<Group> groups)
    {
        var allUserIds = groups.SelectMany(g => g.Members.Select(m => m.UserId)).Distinct().ToList();
        var displayNames = await _clerkService.GetUserDisplayNamesAsync(allUserIds);

        return groups.Select(group => new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            InviteCode = group.InviteCode,
            LockMode = group.LockMode,
            AdminUserId = group.AdminUserId,
            CreatedAt = group.CreatedAt,
            PredictionsLocked = group.PredictionsLocked,
            LockedAt = group.LockedAt,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                Id = m.Id,
                GroupId = m.GroupId,
                UserId = m.UserId,
                DisplayName = displayNames.GetValueOrDefault(m.UserId, m.UserId),
                IsAdmin = m.UserId == group.AdminUserId,
                JoinedAt = m.JoinedAt
            }).ToList()
        }).ToList();
    }
}

public record CreateGroupRequest(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Group name must be 1-100 characters")]
    [RegularExpression(@"^[^<>]*$", ErrorMessage = "Group name contains invalid characters")]
    string Name,
    
    [RegularExpression(@"^(admin|system|hybrid)$", ErrorMessage = "Lock mode must be 'admin', 'system', or 'hybrid'")]
    string LockMode
);

public record RenameGroupRequest(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Group name must be 1-100 characters")]
    [RegularExpression(@"^[^<>]*$", ErrorMessage = "Group name contains invalid characters")]
    string Name
);
