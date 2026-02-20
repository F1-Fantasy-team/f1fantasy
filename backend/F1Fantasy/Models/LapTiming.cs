namespace F1Fantasy.Models;

public class LapTiming
{
    public int Id { get; set; } // Auto-increment primary key
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string LapNumber { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

// API Response models for Lap timing endpoint
public class LapTimingApiResponse
{
    public LapTimingMRData? MRData { get; set; }
}

public class LapTimingMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public LapTimingRaceTable? RaceTable { get; set; }
}

public class LapTimingRaceTable
{
    public string? Season { get; set; }
    public string? Round { get; set; }
    public List<RaceWithLaps>? Races { get; set; }
}

public class RaceWithLaps
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public Circuit? Circuit { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public List<Lap>? Laps { get; set; }
}

public class Lap
{
    public string Number { get; set; } = string.Empty;
    public List<LapTiming>? Timings { get; set; }
}
