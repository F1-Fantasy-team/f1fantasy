namespace F1Fantasy.Models;

public class Race
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public Circuit Circuit { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public Session? FirstPractice { get; set; }
    public Session? SecondPractice { get; set; }
    public Session? ThirdPractice { get; set; }
    public Session? Qualifying { get; set; }
    public Session? Sprint { get; set; }
    public Session? SprintQualifying { get; set; }
}

public class Circuit
{
    public string CircuitId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string CircuitName { get; set; } = string.Empty;
    public Location Location { get; set; } = new();
}

public class Location
{
    public string Lat { get; set; } = string.Empty;
    public string Long { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public class Session
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

// API Response models
public class ApiResponse
{
    public MRData? MRData { get; set; }
}

public class MRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public RaceTable? RaceTable { get; set; }
}

public class RaceTable
{
    public string? Season { get; set; }
    public List<Race>? Races { get; set; }
}