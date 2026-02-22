using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class ConstructorChampionshipPrediction
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    // Ranked constructor IDs in order (stored as JSON, validated to match all constructors)
    public required List<string> RankedConstructorIds { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
