using F1Fantasy.Models;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
    private readonly PredictionService _predictionService;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(GroupService groupService, ClerkService clerkService, PredictionService predictionService, ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _clerkService = clerkService;
        _predictionService = predictionService;
        _logger = logger;
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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var userId = GetUserId();
            _logger.LogInformation("[GetMyGroups] Start - UserId: {UserId} - Elapsed: {Elapsed}ms", userId, stopwatch.ElapsedMilliseconds);
            
            var groups = await _groupService.GetUserGroupsAsync(userId);
            _logger.LogInformation("[GetMyGroups] After GetUserGroupsAsync - GroupCount: {Count} - Elapsed: {Elapsed}ms", groups.Count, stopwatch.ElapsedMilliseconds);
            
            stopwatch.Stop();
            _logger.LogInformation("[GetMyGroups] Complete - Total: {Total}ms", stopwatch.ElapsedMilliseconds);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[GetMyGroups] Error after {Elapsed}ms: {Message}", stopwatch.ElapsedMilliseconds, ex.Message);
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
            var groupDto = await EnrichGroupWithMemberNamesAndPredictionsAsync(group);
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
            var groupDto = await EnrichGroupWithMemberNamesAndPredictionsAsync(group);
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

    private async Task<GroupDto> EnrichGroupWithMemberNamesAndPredictionsAsync(Group group)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("[EnrichGroupWithMemberNamesAndPredictionsAsync] Start - GroupId: {GroupId}, MemberCount: {Count}", 
            group.Id, group.Members.Count);
        
        var userIds = group.Members.Select(m => m.UserId).ToList();
        
        // Fetch display names
        var displayNamesTask = _clerkService.GetUserDisplayNamesAsync(userIds);
        
        // Fetch ALL predictions for the group in one bulk operation (7 parallel queries)
        var predictionsTask = _predictionService.GetAllPredictionsForGroupAsync(group.Id);
        
        await Task.WhenAll(displayNamesTask, predictionsTask);
        
        var displayNames = await displayNamesTask;
        var allPredictions = await predictionsTask;
        
        _logger.LogInformation("[EnrichGroupWithMemberNamesAndPredictionsAsync] After bulk fetch - Elapsed: {Elapsed}ms", 
            stopwatch.ElapsedMilliseconds);

        var members = group.Members.Select(member =>
        {
            var userPredictions = allPredictions.GetValueOrDefault(member.UserId);
            
            return new GroupMemberDto
            {
                Id = member.Id,
                GroupId = member.GroupId,
                UserId = member.UserId,
                DisplayName = displayNames.GetValueOrDefault(member.UserId, member.UserId),
                IsAdmin = member.UserId == group.AdminUserId,
                JoinedAt = member.JoinedAt,
                DriverChampionship = userPredictions?.DriverChampionship,
                ConstructorChampionship = userPredictions?.ConstructorChampionship,
                DriverDraft = userPredictions?.DriverDraft,
                Destructor = userPredictions?.Destructor,
                MrSaturday = userPredictions?.MrSaturday,
                ZeroPointer = userPredictions?.ZeroPointer,
                Wildcard = userPredictions?.Wildcard
            };
        }).ToList();

        stopwatch.Stop();
        _logger.LogInformation("[EnrichGroupWithMemberNamesAndPredictionsAsync] Complete - Total: {Elapsed}ms", 
            stopwatch.ElapsedMilliseconds);

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
            Members = members
        };
    }

    private async Task<List<GroupDto>> EnrichGroupsWithMemberNamesAsync(List<Group> groups)
    {
        var enrichStopwatch = Stopwatch.StartNew();
        var allUserIds = groups.SelectMany(g => g.Members.Select(m => m.UserId)).Distinct().ToList();
        _logger.LogInformation("[EnrichGroupsWithMemberNamesAsync] Distinct UserIds: {Count}, Elapsed: {Elapsed}ms", allUserIds.Count, enrichStopwatch.ElapsedMilliseconds);
        
        var displayNames = await _clerkService.GetUserDisplayNamesAsync(allUserIds);
        _logger.LogInformation("[EnrichGroupsWithMemberNamesAsync] After ClerkService call - DisplayNames: {Count}, Elapsed: {Elapsed}ms", displayNames.Count, enrichStopwatch.ElapsedMilliseconds);

        var result = groups.Select(group => new GroupDto
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
        
        enrichStopwatch.Stop();
        _logger.LogInformation("[EnrichGroupsWithMemberNamesAsync] Complete - Total: {Elapsed}ms", enrichStopwatch.ElapsedMilliseconds);
        return result;
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
