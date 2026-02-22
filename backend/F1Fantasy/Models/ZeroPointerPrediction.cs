using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class ZeroPointerPrediction
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    // List of drivers predicted to score 0 championship points (can be empty or any number of drivers)
    public List<string> DriverIds { get; set; } = new List<string>();
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    [JsonIgnore]
    public Group Group { get; set; } = null!;
}
