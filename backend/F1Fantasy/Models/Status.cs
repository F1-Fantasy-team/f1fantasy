using System.Text.Json.Serialization;

namespace F1Fantasy.Models;

public class Status
{
    public string StatusId { get; set; } = string.Empty; // Primary key (e.g., "1", "11", "5")
    public string StatusText { get; set; } = string.Empty; // e.g., "Finished", "Engine", "+1 Lap"
    public string Count { get; set; } = string.Empty; // Usage count/frequency
}

// API Response Models
public class StatusApiResponse
{
    [JsonPropertyName("MRData")]
    public StatusMRData MRData { get; set; } = new();
}

public class StatusMRData
{
    [JsonPropertyName("StatusTable")]
    public StatusTable StatusTable { get; set; } = new();
}

public class StatusTable
{
    [JsonPropertyName("Status")]
    public List<StatusEntry> Status { get; set; } = new();
}

public class StatusEntry
{
    [JsonPropertyName("statusId")]
    public string StatusId { get; set; } = string.Empty;
    
    [JsonPropertyName("count")]
    public string Count { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string StatusText { get; set; } = string.Empty;
}
