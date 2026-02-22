namespace F1Fantasy.Models;

public class Driver
{
    public string DriverId { get; set; } = string.Empty;
    public string PermanentNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public List<string> ActiveSeasons { get; set; } = new List<string>();
}

// API Response models for Drivers endpoint
public class DriverApiResponse
{
    public DriverMRData? MRData { get; set; }
}

public class DriverMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public DriverTable? DriverTable { get; set; }
}

public class DriverTable
{
    public string? Season { get; set; }
    public List<Driver>? Drivers { get; set; }
}
