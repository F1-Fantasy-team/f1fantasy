namespace F1Fantasy.Models;

// Circuit and Location models are defined in Race.cs and shared across the application

// API Response models for Circuits endpoint
public class CircuitApiResponse
{
    public CircuitMRData? MRData { get; set; }
}

public class CircuitMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public CircuitTable? CircuitTable { get; set; }
}

public class CircuitTable
{
    public List<Circuit>? Circuits { get; set; }
}
