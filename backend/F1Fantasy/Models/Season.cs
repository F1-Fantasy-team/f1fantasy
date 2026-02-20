namespace F1Fantasy.Models;

public class Season
{
    public string Year { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

// API Response models for Seasons
public class SeasonApiResponse
{
    public SeasonMRData? MRData { get; set; }
}

public class SeasonMRData
{
    public string? Xmlns { get; set; }
    public string? Series { get; set; }
    public string? Url { get; set; }
    public string? Limit { get; set; }
    public string? Offset { get; set; }
    public string? Total { get; set; }
    public SeasonTable? SeasonTable { get; set; }
}

public class SeasonTable
{
    public List<SeasonData>? Seasons { get; set; }
}

public class SeasonData
{
    public string Season { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
