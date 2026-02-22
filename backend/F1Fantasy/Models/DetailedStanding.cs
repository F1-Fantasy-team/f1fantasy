namespace F1Fantasy.Models;

public class DetailedStanding
{
    public required string UserId { get; set; }
    public int GroupId { get; set; }
    public int TotalScore { get; set; }
    public int Rank { get; set; }
    
    // Overall category totals
    public Dictionary<string, int> CategoryTotals { get; set; } = new();
    
    // Round-by-round breakdown
    public List<RoundScore> RoundScores { get; set; } = new();
}

public class RoundScore
{
    public string Round { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    
    // Category scores for this specific round
    public Dictionary<string, int> CategoryScores { get; set; } = new();
    
    // Cumulative score up to and including this round
    public int CumulativeScore { get; set; }
}

public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public List<RoundDetail> RoundDetails { get; set; } = new();
}

public class RoundDetail
{
    public string Round { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public int Points { get; set; }
    public string? Details { get; set; } // e.g., "Position 1: Correct (+10)", "DNF detected (+20)"
}
