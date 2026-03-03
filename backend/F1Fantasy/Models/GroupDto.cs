namespace F1Fantasy.Models;

public class GroupDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string InviteCode { get; set; }
    public required string LockMode { get; set; }
    public required string AdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool PredictionsLocked { get; set; }
    public DateTime? LockedAt { get; set; }
    public List<GroupMemberDto> Members { get; set; } = new();
}

public class GroupMemberDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public required string UserId { get; set; }
    public required string DisplayName { get; set; } // Name from Clerk (fallback to username)
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
}
