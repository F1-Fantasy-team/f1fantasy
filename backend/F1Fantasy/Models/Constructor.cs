namespace F1Fantasy.Models;

public class Constructor
{
    public string ConstructorId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public List<string> ActiveSeasons { get; set; } = new List<string>();
}

// API Response models for Constructors endpoint
public class ConstructorApiResponse
{
    public ConstructorMRData? MRData { get; set; }
}

public class ConstructorMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public ConstructorTable? ConstructorTable { get; set; }
}

public class ConstructorTable
{
    public string? Season { get; set; }
    public List<Constructor>? Constructors { get; set; }
}
