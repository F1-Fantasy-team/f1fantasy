namespace F1Fantasy.Models;

public class Qualifying
{
    public int Id { get; set; } // Auto-increment primary key
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public Driver Driver { get; set; } = new();
    public string ConstructorId { get; set; } = string.Empty;
    public Constructor Constructor { get; set; } = new();
    public string? Q1 { get; set; }
    public string? Q2 { get; set; }
    public string? Q3 { get; set; }
}

// API Response models for Qualifying endpoint
public class QualifyingApiResponse
{
    public QualifyingMRData? MRData { get; set; }
}

public class QualifyingMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public QualifyingRaceTable? RaceTable { get; set; }
}

public class QualifyingRaceTable
{
    public string? Season { get; set; }
    public List<RaceWithQualifying>? Races { get; set; }
}

public class RaceWithQualifying
{
    public string Season { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string RaceName { get; set; } = string.Empty;
    public Circuit Circuit { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public List<Qualifying>? QualifyingResults { get; set; }
}
