namespace F1Fantasy.Middleware;

/// <summary>
/// Adds caching headers to GET requests to reduce bandwidth and database load
/// </summary>
public class CacheHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public CacheHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (context.Request.Method == HttpMethods.Get)
        {
            // Add cache headers based on endpoint
            var path = context.Request.Path.ToString().ToLower();

            if (ShouldCacheAggressively(path))
            {
                // F1 data changes infrequently - cache for 1 hour
                context.Response.Headers.CacheControl = "public, max-age=3600";
            }
            else if (ShouldCacheModerately(path))
            {
                // Fantasy league data - cache for 5 minutes
                context.Response.Headers.CacheControl = "public, max-age=300";
            }
            else if (ShouldCacheMinimally(path))
            {
                // Standings/predictions - cache for 30 seconds
                context.Response.Headers.CacheControl = "public, max-age=30";
            }
            else
            {
                // Default: no cache for dynamic content
                context.Response.Headers.CacheControl = "no-cache, must-revalidate";
            }
        }

        await _next(context);
    }

    /// <summary>
    /// F1 historical data rarely changes - aggressive caching (1 hour)
    /// </summary>
    private bool ShouldCacheAggressively(string path)
    {
        return path.Contains("/driver/") ||
               path.Contains("/constructor/") ||
               path.Contains("/circuit/") ||
               path.Contains("/season/") ||
               path.Contains("/race/") ||
               path.Contains("/qualifying/") ||
               path.Contains("/result/") ||
               path.Contains("/pitstop/") ||
               path.Contains("/laptiming/");
    }

    /// <summary>
    /// Group and member data changes occasionally - moderate caching (5 min)
    /// </summary>
    private bool ShouldCacheModerately(string path)
    {
        // Disabled caching for /groups due to delete/update issues
        // Users were seeing deleted groups due to 5-minute cache
        return false; // path.Contains("/groups") && !path.Contains("/standings");
    }

    /// <summary>
    /// Standings and predictions change frequently - minimal caching (30 sec)
    /// </summary>
    private bool ShouldCacheMinimally(string path)
    {
        return path.Contains("/standings") || 
               path.Contains("/predictions");
    }
}
