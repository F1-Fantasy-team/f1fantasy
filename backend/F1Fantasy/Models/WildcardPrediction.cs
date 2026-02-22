using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class WildcardPrediction
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    public string? Statement { get; set; }
    public int? PointsPotential { get; set; } // Admin sets 100-200
    public bool? Fullfilled { get; set; } // Admin marks true/false
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
