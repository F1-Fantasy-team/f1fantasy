using F1Fantasy.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Concurrent;
using System.Threading.RateLimiting;

namespace F1Fantasy.Middleware;

public class IpBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpBlacklistMiddleware> _logger;

    public IpBlacklistMiddleware(RequestDelegate next, ILogger<IpBlacklistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIpBlacklistService blacklistService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        
        if (string.IsNullOrEmpty(ipAddress))
        {
            await _next(context);
            return;
        }

        if (blacklistService.IsBlacklisted(ipAddress))
        {
            _logger.LogWarning("Blocked request from blacklisted IP: {IpAddress}", ipAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "Access denied",
                message = "Your IP address has been blocked due to suspicious activity. Contact support if you believe this is an error."
            });
            return;
        }

        await _next(context);
    }
}

public class RateLimitViolationMonitor
{
    private readonly ConcurrentDictionary<string, ViolationTracker> _violations = new();
    private readonly ILogger<RateLimitViolationMonitor> _logger;
    private readonly IIpBlacklistService _blacklistService;

    // Track IPs that exceed rate limits excessively
    private const int VIOLATION_THRESHOLD = 10; // Number of rate limit violations before blacklisting
    private const int VIOLATION_WINDOW_MINUTES = 5; // Time window to track violations

    public RateLimitViolationMonitor(
        ILogger<RateLimitViolationMonitor> logger,
        IIpBlacklistService blacklistService)
    {
        _logger = logger;
        _blacklistService = blacklistService;
    }

    public void RecordViolation(string ipAddress)
    {
        var tracker = _violations.GetOrAdd(ipAddress, _ => new ViolationTracker());
        
        lock (tracker)
        {
            // Remove old violations outside the window
            tracker.Violations.RemoveAll(v => v < DateTime.UtcNow.AddMinutes(-VIOLATION_WINDOW_MINUTES));
            
            // Add new violation
            tracker.Violations.Add(DateTime.UtcNow);

            // Check if threshold exceeded
            if (tracker.Violations.Count >= VIOLATION_THRESHOLD)
            {
                _logger.LogWarning(
                    "IP {IpAddress} exceeded rate limit {Count} times in {Minutes} minutes. Auto-blacklisting.",
                    ipAddress, tracker.Violations.Count, VIOLATION_WINDOW_MINUTES);

                // Auto-blacklist for 1 hour
                _blacklistService.Blacklist(
                    ipAddress, 
                    $"Auto-blacklisted: {tracker.Violations.Count} rate limit violations in {VIOLATION_WINDOW_MINUTES} minutes",
                    TimeSpan.FromHours(1));

                // Clear violations after blacklisting
                tracker.Violations.Clear();
            }
        }
    }

    private class ViolationTracker
    {
        public List<DateTime> Violations { get; } = new();
    }
}

// Extension to access violations in rate limit rejection
public static class RateLimitExtensions
{
    public static void ConfigureRateLimitRejection(this RateLimiterOptions options)
    {
        options.OnRejected = async (context, cancellationToken) =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var monitor = context.HttpContext.RequestServices.GetRequiredService<RateLimitViolationMonitor>();
            
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                monitor.RecordViolation(ipAddress);
            }

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            
            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterMetadata)
                ? retryAfterMetadata.TotalSeconds
                : 60;

            context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = "Too many requests. Please slow down and try again later.",
                retryAfter = $"{retryAfter} seconds"
            }, cancellationToken);

            logger.LogWarning("Rate limit exceeded for IP: {IpAddress}, Endpoint: {Path}", 
                ipAddress, context.HttpContext.Request.Path);
        };
    }
}
