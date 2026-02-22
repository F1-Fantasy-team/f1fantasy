namespace F1Fantasy.Models;

public class Standing
{
    public int Id { get; set; }
    public required string UserId { get; set; } // Clerk user ID
    public int GroupId { get; set; }
    
    public int TotalScore { get; set; }
    public int Rank { get; set; }
    
    // Category scores stored as JSON
    public string? CategoryScoresJson { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public Group Group { get; set; } = null!;
}
