namespace F1Fantasy.Models;

public class UserDisplayNameCache
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
