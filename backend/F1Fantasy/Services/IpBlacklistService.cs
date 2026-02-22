using System.Collections.Concurrent;

namespace F1Fantasy.Services;

public interface IIpBlacklistService
{
    bool IsBlacklisted(string ipAddress);
    void Blacklist(string ipAddress, string reason, TimeSpan? duration = null);
    void Unblacklist(string ipAddress);
    Dictionary<string, BlacklistEntry> GetBlacklistedIps();
}

public class BlacklistEntry
{
    public string IpAddress { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime BlacklistedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class IpBlacklistService : IIpBlacklistService
{
    private readonly ConcurrentDictionary<string, BlacklistEntry> _blacklist = new();
    private readonly ILogger<IpBlacklistService> _logger;

    public IpBlacklistService(ILogger<IpBlacklistService> logger)
    {
        _logger = logger;
    }

    public bool IsBlacklisted(string ipAddress)
    {
        if (_blacklist.TryGetValue(ipAddress, out var entry))
        {
            // Check if temporary ban has expired
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
            {
                _blacklist.TryRemove(ipAddress, out _);
                _logger.LogInformation("Temporary blacklist expired for IP: {IpAddress}", ipAddress);
                return false;
            }
            
            return true;
        }
        
        return false;
    }

    public void Blacklist(string ipAddress, string reason, TimeSpan? duration = null)
    {
        var entry = new BlacklistEntry
        {
            IpAddress = ipAddress,
            Reason = reason,
            BlacklistedAt = DateTime.UtcNow,
            ExpiresAt = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null
        };

        _blacklist[ipAddress] = entry;
        
        var durationType = duration.HasValue ? $"for {duration.Value.TotalMinutes} minutes" : "permanently";
        _logger.LogWarning("IP {IpAddress} blacklisted {Duration}. Reason: {Reason}", 
            ipAddress, durationType, reason);
    }

    public void Unblacklist(string ipAddress)
    {
        if (_blacklist.TryRemove(ipAddress, out _))
        {
            _logger.LogInformation("IP {IpAddress} removed from blacklist", ipAddress);
        }
    }

    public Dictionary<string, BlacklistEntry> GetBlacklistedIps()
    {
        // Clean up expired entries first
        var expiredIps = _blacklist
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var ip in expiredIps)
        {
            _blacklist.TryRemove(ip, out _);
        }

        return new Dictionary<string, BlacklistEntry>(_blacklist);
    }
}
