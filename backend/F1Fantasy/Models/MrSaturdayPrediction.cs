using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class MrSaturdayPrediction
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    // Up to 2 drivers who will win most qualifying sessions (nullable for partial drafts)
    public string? Driver1Id { get; set; }
    public string? Driver2Id { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
