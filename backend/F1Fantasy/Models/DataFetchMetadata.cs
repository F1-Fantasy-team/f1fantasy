namespace F1Fantasy.Models;

public class DataFetchMetadata
{
    public int Id { get; set; }
    public string Season { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty; // "Races", "Results", "Standings", etc.
    public DateTime LastFetchedAt { get; set; }
    public int? LatestRoundAtFetch { get; set; }
    public bool FetchSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}
