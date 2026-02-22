using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class ConstructorStanding
{
    // Composite primary key: Season + ConstructorId
    public string Season { get; set; } = string.Empty;
    public string ConstructorId { get; set; } = string.Empty;
    
    public string Round { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PositionText { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string Wins { get; set; } = string.Empty;
    
    // Navigation property (not stored in DB)
    public Constructor? Constructor { get; set; }
}

// API Response Models
public class ConstructorStandingApiResponse
{
    [JsonPropertyName("MRData")]
    public ConstructorStandingMRData MRData { get; set; } = new();
}

public class ConstructorStandingMRData
{
    [JsonPropertyName("StandingsTable")]
    public ConstructorStandingsTable StandingsTable { get; set; } = new();
}

public class ConstructorStandingsTable
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = string.Empty;
    
    [JsonPropertyName("round")]
    public string Round { get; set; } = string.Empty;
    
    [JsonPropertyName("StandingsLists")]
    public List<ConstructorStandingsList> StandingsLists { get; set; } = new();
}

public class ConstructorStandingsList
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = string.Empty;
    
    [JsonPropertyName("round")]
    public string Round { get; set; } = string.Empty;
    
    [JsonPropertyName("ConstructorStandings")]
    public List<ConstructorStandingEntry> ConstructorStandings { get; set; } = new();
}

public class ConstructorStandingEntry
{
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;
    
    [JsonPropertyName("positionText")]
    public string PositionText { get; set; } = string.Empty;
    
    [JsonPropertyName("points")]
    public string Points { get; set; } = string.Empty;
    
    [JsonPropertyName("wins")]
    public string Wins { get; set; } = string.Empty;
    
    [JsonPropertyName("Constructor")]
    public Constructor Constructor { get; set; } = new();
}
