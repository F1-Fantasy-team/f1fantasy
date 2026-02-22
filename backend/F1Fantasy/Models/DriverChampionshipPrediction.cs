using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class DriverChampionshipPrediction
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    // Ranked driver IDs in order (stored as JSON, validated to match all active drivers 20-22)
    public required List<string> RankedDriverIds { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
