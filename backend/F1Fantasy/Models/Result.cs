namespace F1Fantasy.Models;

public class Result
{
    public int Id { get; set; } // Auto-increment primary key
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string PositionText { get; set; } = string.Empty;
    public string Points { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Driver Driver { get; set; } = new();
    public string ConstructorId { get; set; } = string.Empty;
    public Constructor Constructor { get; set; } = new();
    public string Grid { get; set; } = string.Empty;
    public string Laps { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Status text (e.g., "Finished", "Engine")
    public string? StatusId { get; set; } // Foreign key to Status table (e.g., "1", "5")
    public ResultTime? Time { get; set; }
    public FastestLap? FastestLap { get; set; }
    public bool IsSprint { get; set; } = false; // Indicates if this is a sprint race result
}

public class ResultTime
{
    public string Millis { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

public class FastestLap
{
    public string Rank { get; set; } = string.Empty;
    public string Lap { get; set; } = string.Empty;
    public LapTime Time { get; set; } = new();
    public AverageSpeed? AverageSpeed { get; set; }
}

public class LapTime
{
    public string Time { get; set; } = string.Empty;
}

public class AverageSpeed
{
    public string Units { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
}

// API Response models for Results endpoint
public class ResultApiResponse
{
    public ResultMRData? MRData { get; set; }
}

public class ResultMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public ResultRaceTable? RaceTable { get; set; }
}

public class ResultRaceTable
{
    public string? Season { get; set; }
    public List<RaceWithResults>? Races { get; set; }
}

public class RaceWithResults
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public Circuit Circuit { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public List<Result>? Results { get; set; }
    public List<Result>? SprintResults { get; set; } // Sprint race results
}
