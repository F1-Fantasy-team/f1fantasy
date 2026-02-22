namespace F1Fantasy.Models;

public class Group
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string InviteCode { get; set; }
    public required string LockMode { get; set; } // "admin", "system", "hybrid"
    public required string AdminUserId { get; set; } // Clerk user ID
    public DateTime CreatedAt { get; set; }
    
    // Prediction lock (group-wide for all predictions)
    public bool PredictionsLocked { get; set; }
    public DateTime? LockedAt { get; set; }

    // Navigation properties
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
