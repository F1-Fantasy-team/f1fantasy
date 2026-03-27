using F1Fantasy.Repository;

namespace F1Fantasy.Middleware;

/// <summary>
/// Adds caching headers to GET requests to reduce bandwidth and database load
/// Smart caching: disables cache when data was recently fetched to prevent stale cached responses
/// </summary>
public class CacheHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public CacheHeaderMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (context.Request.Method == HttpMethods.Get)
        {
            // Add cache headers based on endpoint
            var path = context.Request.Path.ToString().ToLower();

            // Check if data was recently fetched for standings-related endpoints
            bool isRecentlyFetched = false;
            if (path.Contains("/standings"))
            {
                isRecentlyFetched = await IsDataRecentlyFetchedAsync(context);
            }

            if (isRecentlyFetched)
            {
                // Data just fetched - force revalidation to prevent serving stale cache
                context.Response.Headers.CacheControl = "no-cache, must-revalidate";
                context.Response.Headers.Pragma = "no-cache";
            }
            else if (ShouldCacheAggressively(path))
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
    /// Check if standings data was recently fetched (within last 2 minutes)
    /// to prevent serving stale cached responses
    /// </summary>
    private async Task<bool> IsDataRecentlyFetchedAsync(HttpContext context)
    {
        try
        {
            // Get season from query string
            if (!context.Request.Query.TryGetValue("season", out var seasonValue))
            {
                return false;
            }

            var season = seasonValue.ToString();
            
            using var scope = _scopeFactory.CreateScope();
            var metadataRepo = scope.ServiceProvider.GetRequiredService<DataFetchMetadataRepository>();

            // Check both driver and constructor standings metadata
            var dataTypes = new[] { "DriverStandings", "ConstructorStandings" };
            
            foreach (var dataType in dataTypes)
            {
                var metadata = await metadataRepo.GetMetadataAsync(season, dataType);
                
                if (metadata != null && metadata.FetchSuccessful)
                {
                    var age = DateTime.UtcNow - metadata.LastFetchedAt;
                    
                    // If data was fetched in the last 2 minutes, it's fresh - don't cache
                    if (age.TotalMinutes < 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch
        {
            // If metadata check fails, play it safe and don't cache
            return true;
        }
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
