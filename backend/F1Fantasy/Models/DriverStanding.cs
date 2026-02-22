namespace F1Fantasy.Models;

public class DriverStanding
{
    public string Season { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PositionText { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string Wins { get; set; } = string.Empty;
    public string ConstructorId { get; set; } = string.Empty; // Primary constructor for the driver
    
    // Navigation properties (ignored in DB, used for API responses)
    public Driver? Driver { get; set; }
    public Constructor? Constructor { get; set; }
}

// API Response models for Driver Standings endpoint
public class DriverStandingApiResponse
{
    public DriverStandingMRData? MRData { get; set; }
}

public class DriverStandingMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public StandingsTable? StandingsTable { get; set; }
}

public class StandingsTable
{
    public string? Season { get; set; }
    public string? Round { get; set; }
    public List<StandingsList>? StandingsLists { get; set; }
}

public class StandingsList
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public List<DriverStandingEntry>? DriverStandings { get; set; }
}

public class DriverStandingEntry
{
    public string Position { get; set; } = string.Empty;
    public string PositionText { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string Wins { get; set; } = string.Empty;
    public Driver? Driver { get; set; }
    public List<Constructor>? Constructors { get; set; }
}
