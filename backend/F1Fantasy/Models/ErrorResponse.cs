namespace F1Fantasy.Models;

public class ErrorResponse
{
    public string? Error { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? StackTrace { get; set; }
    public string? RetryAfter { get; set; }
    public string? RequestId { get; set; }
    public int StatusCode { get; set; }
    public string Path { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
