namespace F1Fantasy.Models;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? StackTrace { get; set; }
    public int StatusCode { get; set; }
    public string Path { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
