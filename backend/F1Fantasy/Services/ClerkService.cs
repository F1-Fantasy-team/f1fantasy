using Clerk.BackendAPI;
using Clerk.BackendAPI.Models.Operations;
using F1Fantasy.Data;
using F1Fantasy.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace F1Fantasy.Services;

public class ClerkService
{
    private readonly ClerkBackendApi _clerkClient;
    private readonly ILogger<ClerkService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<F1FantasyDbContext> _contextFactory;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7); // Database cache lasts 7 days
    private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromMinutes(30); // Memory cache lasts 30 minutes

    public ClerkService(ILogger<ClerkService> logger, IMemoryCache cache, IDbContextFactory<F1FantasyDbContext> contextFactory)
    {
        _logger = logger;
        _cache = cache;
        _contextFactory = contextFactory;
        var clerkSecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY");
        
        if (string.IsNullOrEmpty(clerkSecretKey))
        {
            throw new InvalidOperationException("CLERK_SECRET_KEY environment variable is not set.");
        }

        _clerkClient = new ClerkBackendApi(clerkSecretKey);
    }

    /// <summary>
    /// Fetches user display name from Clerk. Returns first name + last name if available, otherwise username.
    /// Falls back to the user ID if all else fails. Results are cached in memory and database for improved performance.
    /// Uses a stale-while-revalidate strategy: if a cached name exists, it returns immediately and refreshes in the background.
    /// </summary>
    public async Task<string> GetUserDisplayNameAsync(string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Try to get from memory cache first (fastest)
        var memoryCacheKey = $"clerk_user_{userId}";
        if (_cache.TryGetValue<string>(memoryCacheKey, out var cachedName) && cachedName != null)
        {
            _logger.LogDebug("[GetUserDisplayNameAsync] Retrieved from memory cache - UserId: {UserId}, Elapsed: {Elapsed}ms", userId, stopwatch.ElapsedMilliseconds);
            return cachedName;
        }

        // Try to get from database cache - handle gracefully if table doesn't exist
        UserDisplayNameCache? dbCache = null;
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            dbCache = await context.UserDisplayNameCache
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
                
            if (dbCache != null)
            {
                // Cache exists - return it immediately and refresh in background
                _cache.Set(memoryCacheKey, dbCache.DisplayName, MemoryCacheDuration);
                _logger.LogInformation("[GetUserDisplayNameAsync] Retrieved from database cache - UserId: {UserId}, Expired: {IsExpired}, Elapsed: {Elapsed}ms", 
                    userId, dbCache.ExpiresAt <= DateTime.UtcNow, stopwatch.ElapsedMilliseconds);
                
                // Fire background refresh (don't await)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogDebug("[GetUserDisplayNameAsync] Background refresh started for UserId: {UserId}", userId);
                        await FetchAndCacheFromClerkApiAsync(userId);
                    }
                    catch (Exception bgEx)
                    {
                        _logger.LogWarning(bgEx, "[GetUserDisplayNameAsync] Background refresh failed for UserId: {UserId}", userId);
                    }
                });
                
                return dbCache.DisplayName;
            }
        }
        catch (Exception dbEx)
        {
            // Database cache not available (table might not exist yet), continue to API fetch
            _logger.LogWarning(dbEx, "[GetUserDisplayNameAsync] Database cache unavailable for UserId: {UserId}, falling back to API", userId);
        }

        // No cache exists - fetch from Clerk API synchronously
        _logger.LogInformation("[GetUserDisplayNameAsync] No cache exists, fetching from Clerk API - UserId: {UserId}", userId);
        stopwatch.Stop();
        return await FetchAndCacheFromClerkApiAsync(userId);
    }
    
    /// <summary>
    /// Fetches user display name from Clerk API and updates cache
    /// </summary>
    private async Task<string> FetchAndCacheFromClerkApiAsync(string userId)
    {
        try
        {
            var response = await _clerkClient.Users.GetAsync(userId);
            
            if (response?.User == null)
            {
                _logger.LogWarning("[FetchAndCacheFromClerkApiAsync] User {UserId} not found in Clerk", userId);
                var fallbackId = userId;
                await UpdateCacheAsync(userId, fallbackId);
                return fallbackId;
            }

            var user = response.User;

            // Try to build full name from first and last name
            var firstName = user.FirstName?.Trim();
            var lastName = user.LastName?.Trim();

            string displayName;
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                displayName = $"{firstName} {lastName}";
            }
            else if (!string.IsNullOrEmpty(firstName))
            {
                displayName = firstName;
            }
            else if (!string.IsNullOrEmpty(lastName))
            {
                displayName = lastName;
            }
            else if (!string.IsNullOrEmpty(user.Username))
            {
                displayName = user.Username;
            }
            else
            {
                _logger.LogWarning("[FetchAndCacheFromClerkApiAsync] No name or username found for user {UserId}, using ID as display name", userId);
                displayName = userId;
                // Cache userId fallbacks for shorter time (1 hour) in case user updates their profile
                await UpdateCacheAsync(userId, displayName, TimeSpan.FromHours(1));
                _logger.LogInformation("[FetchAndCacheFromClerkApiAsync] Fetched from Clerk API - UserId: {UserId}, DisplayName: {DisplayName} (fallback)", userId, displayName);
                return displayName;
            }

            // Update both caches with standard duration
            await UpdateCacheAsync(userId, displayName);
            _logger.LogInformation("[FetchAndCacheFromClerkApiAsync] Fetched from Clerk API - UserId: {UserId}, DisplayName: {DisplayName}", userId, displayName);
            return displayName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FetchAndCacheFromClerkApiAsync] Error fetching user {UserId} from Clerk: {Message}", userId, ex.Message);
            var fallbackId = userId;
            // Cache failed lookups for a shorter time
            await UpdateCacheAsync(userId, fallbackId, TimeSpan.FromMinutes(5));
            return fallbackId;
        }
    }
    
    private async Task UpdateCacheAsync(string userId, string displayName, TimeSpan? duration = null)
    {
        var cacheDuration = duration ?? CacheDuration;
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(cacheDuration);
        
        // Update memory cache
        _cache.Set($"clerk_user_{userId}", displayName, duration ?? MemoryCacheDuration);
        
        // Update database cache
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existingCache = await context.UserDisplayNameCache.FindAsync(userId);
            if (existingCache != null)
            {
                existingCache.DisplayName = displayName;
                existingCache.CachedAt = now;
                existingCache.ExpiresAt = expiresAt;
            }
            else
            {
                context.UserDisplayNameCache.Add(new UserDisplayNameCache
                {
                    UserId = userId,
                    DisplayName = displayName,
                    CachedAt = now,
                    ExpiresAt = expiresAt
                });
            }
            
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[UpdateCacheAsync] Failed to update database cache for UserId: {UserId}", userId);
        }
    }

    /// <summary>
    /// Fetches multiple user display names with optimized batch database lookup.
    /// Uses a stale-while-revalidate strategy: returns cached names immediately and refreshes in background.
    /// </summary>
    public async Task<Dictionary<string, string>> GetUserDisplayNamesAsync(IEnumerable<string> userIds)
    {
        var stopwatch = Stopwatch.StartNew();
        var uniqueUserIds = userIds.Distinct().ToList();
        var result = new Dictionary<string, string>();
        var uncachedUserIds = new List<string>();
        
        _logger.LogInformation("[GetUserDisplayNamesAsync] Fetching {Count} unique user display names", uniqueUserIds.Count);
        
        // First, try to get all from database cache in one query (ignore expiration)
        List<UserDisplayNameCache> cachedUsers;
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            cachedUsers = await context.UserDisplayNameCache
                .AsNoTracking()
                .Where(u => uniqueUserIds.Contains(u.UserId))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GetUserDisplayNamesAsync] Database cache query failed (table may not exist yet), falling back to API");
            cachedUsers = new List<UserDisplayNameCache>();
        }
            
        _logger.LogInformation("[GetUserDisplayNamesAsync] Found {Count} users in database cache - Elapsed: {Elapsed}ms", 
            cachedUsers.Count, stopwatch.ElapsedMilliseconds);
        
        // Separate cached users into fresh and stale
        var staleUserIds = new List<string>();
        foreach (var cached in cachedUsers)
        {
            result[cached.UserId] = cached.DisplayName;
            // Also update memory cache
            _cache.Set($"clerk_user_{cached.UserId}", cached.DisplayName, MemoryCacheDuration);
            
            // Track stale entries for background refresh
            if (cached.ExpiresAt <= DateTime.UtcNow)
            {
                staleUserIds.Add(cached.UserId);
            }
        }
        
        // Fire background refresh for stale cached users (don't await)
        if (staleUserIds.Any())
        {
            _logger.LogInformation("[GetUserDisplayNamesAsync] Triggering background refresh for {Count} stale users", staleUserIds.Count);
            _ = Task.Run(async () =>
            {
                try
                {
                    var tasks = staleUserIds.Select(async userId =>
                    {
                        try
                        {
                            await FetchAndCacheFromClerkApiAsync(userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[GetUserDisplayNamesAsync] Background refresh failed for UserId: {UserId}", userId);
                        }
                    });
                    await Task.WhenAll(tasks);
                }
                catch (Exception bgEx)
                {
                    _logger.LogWarning(bgEx, "[GetUserDisplayNamesAsync] Background refresh batch failed");
                }
            });
        }
        
        // Find which users are not cached at all
        uncachedUserIds = uniqueUserIds.Except(result.Keys).ToList();
        
        if (uncachedUserIds.Any())
        {
            _logger.LogInformation("[GetUserDisplayNamesAsync] Fetching {Count} uncached users from Clerk API", uncachedUserIds.Count);
            
            // Fetch uncached users from Clerk API in parallel (await these)
            var tasks = uncachedUserIds.Select(async userId =>
            {
                var displayName = await FetchAndCacheFromClerkApiAsync(userId);
                return new { UserId = userId, DisplayName = displayName };
            });

            var fetchedResults = await Task.WhenAll(tasks);
            
            foreach (var fetchedResult in fetchedResults)
            {
                result[fetchedResult.UserId] = fetchedResult.DisplayName;
            }
        }
        
        stopwatch.Stop();
        _logger.LogInformation("[GetUserDisplayNamesAsync] Complete - Total: {Total}ms, Cached: {Cached}, Stale: {Stale}, Uncached: {Uncached}", 
            stopwatch.ElapsedMilliseconds, cachedUsers.Count, staleUserIds.Count, uncachedUserIds.Count);
        
        return result;
    }
}
