namespace F1Fantasy.Models;

public class PitStop
{
    public int Id { get; set; } // Auto-increment primary key
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string Lap { get; set; } = string.Empty;
    public string Stop { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}

// API Response models for PitStop endpoint
public class PitStopApiResponse
{
    public PitStopMRData? MRData { get; set; }
}

public class PitStopMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public PitStopRaceTable? RaceTable { get; set; }
}

public class PitStopRaceTable
{
    public string? Season { get; set; }
    public string? Round { get; set; }
    public List<RaceWithPitStops>? Races { get; set; }
}

public class RaceWithPitStops
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public Circuit Circuit { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public List<PitStop>? PitStops { get; set; }
}
