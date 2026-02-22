using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/admin/blacklist")]
[Authorize] // Requires authentication
[EnableRateLimiting("admin")]
public class BlacklistController : ControllerBase
{
    private readonly IIpBlacklistService _blacklistService;
    private readonly ILogger<BlacklistController> _logger;

    public BlacklistController(
        IIpBlacklistService blacklistService,
        ILogger<BlacklistController> logger)
    {
        _blacklistService = blacklistService;
        _logger = logger;
    }

    /// <summary>
    /// Get all blacklisted IPs (Admin only)
    /// </summary>
    [HttpGet]
    public IActionResult GetBlacklistedIps()
    {
        // TODO: Add role-based authorization for admins only
        // For now, any authenticated user can view (you should restrict this)
        
        var blacklist = _blacklistService.GetBlacklistedIps();
        return Ok(blacklist);
    }

    /// <summary>
    /// Blacklist an IP address (Admin only)
    /// </summary>
    [HttpPost]
    public IActionResult BlacklistIp([FromBody] BlacklistRequest request)
    {
        if (string.IsNullOrEmpty(request.IpAddress))
        {
            return BadRequest(new { error = "IP address is required" });
        }

        TimeSpan? duration = request.DurationMinutes.HasValue 
            ? TimeSpan.FromMinutes(request.DurationMinutes.Value) 
            : null;

        _blacklistService.Blacklist(request.IpAddress, request.Reason ?? "Manual blacklist", duration);

        _logger.LogInformation("Admin {UserId} blacklisted IP {IpAddress}", 
            User.FindFirst("sub")?.Value, request.IpAddress);

        return Ok(new { message = $"IP {request.IpAddress} has been blacklisted" });
    }

    /// <summary>
    /// Remove an IP from blacklist (Admin only)
    /// </summary>
    [HttpDelete("{ipAddress}")]
    public IActionResult UnblacklistIp(string ipAddress)
    {
        _blacklistService.Unblacklist(ipAddress);

        _logger.LogInformation("Admin {UserId} removed IP {IpAddress} from blacklist", 
            User.FindFirst("sub")?.Value, ipAddress);

        return Ok(new { message = $"IP {ipAddress} has been removed from blacklist" });
    }
}

public class BlacklistRequest
{
    public string IpAddress { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int? DurationMinutes { get; set; } // null = permanent
}
