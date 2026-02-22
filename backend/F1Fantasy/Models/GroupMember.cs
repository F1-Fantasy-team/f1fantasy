using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public DateTime JoinedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
