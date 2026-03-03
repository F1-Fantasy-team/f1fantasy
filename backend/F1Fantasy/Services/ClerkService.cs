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
    private readonly F1FantasyDbContext _context;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7); // Database cache lasts 7 days
    private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromMinutes(30); // Memory cache lasts 30 minutes

    public ClerkService(ILogger<ClerkService> logger, IMemoryCache cache, F1FantasyDbContext context)
    {
        _logger = logger;
        _cache = cache;
        _context = context;
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

        // Try to get from database cache (still fast)
        var dbCache = await _context.UserDisplayNameCache
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
            
        if (dbCache != null && dbCache.ExpiresAt > DateTime.UtcNow)
        {
            // Cache hit in database, update memory cache and return
            _cache.Set(memoryCacheKey, dbCache.DisplayName, MemoryCacheDuration);
            _logger.LogDebug("[GetUserDisplayNameAsync] Retrieved from database cache - UserId: {UserId}, Elapsed: {Elapsed}ms", userId, stopwatch.ElapsedMilliseconds);
            return dbCache.DisplayName;
        }

        // Cache miss or expired, fetch from Clerk API
        _logger.LogInformation("[GetUserDisplayNameAsync] Cache miss, fetching from Clerk API - UserId: {UserId}", userId);
        
        try
        {
            var response = await _clerkClient.Users.GetAsync(userId);
            
            if (response?.User == null)
            {
                _logger.LogWarning("[GetUserDisplayNameAsync] User {UserId} not found in Clerk", userId);
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
                _logger.LogWarning("[GetUserDisplayNameAsync] No name or username found for user {UserId}, using ID as display name", userId);
                displayName = userId;
            }

            // Update both caches
            await UpdateCacheAsync(userId, displayName);
            stopwatch.Stop();
            _logger.LogInformation("[GetUserDisplayNameAsync] Fetched from Clerk API - UserId: {UserId}, DisplayName: {DisplayName}, Total: {Elapsed}ms", userId, displayName, stopwatch.ElapsedMilliseconds);
            return displayName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetUserDisplayNameAsync] Error fetching user {UserId} from Clerk: {Message}", userId, ex.Message);
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
        var existingCache = await _context.UserDisplayNameCache.FindAsync(userId);
        if (existingCache != null)
        {
            existingCache.DisplayName = displayName;
            existingCache.CachedAt = now;
            existingCache.ExpiresAt = expiresAt;
        }
        else
        {
            _context.UserDisplayNameCache.Add(new UserDisplayNameCache
            {
                UserId = userId,
                DisplayName = displayName,
                CachedAt = now,
                ExpiresAt = expiresAt
            });
        }
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Fetches multiple user display names with optimized batch database lookup
    /// </summary>
    public async Task<Dictionary<string, string>> GetUserDisplayNamesAsync(IEnumerable<string> userIds)
    {
        var stopwatch = Stopwatch.StartNew();
        var uniqueUserIds = userIds.Distinct().ToList();
        var result = new Dictionary<string, string>();
        var uncachedUserIds = new List<string>();
        
        _logger.LogInformation("[GetUserDisplayNamesAsync] Fetching {Count} unique user display names", uniqueUserIds.Count);
        
        // First, try to get all from database cache in one query
        var cachedUsers = await _context.UserDisplayNameCache
            .AsNoTracking()
            .Where(u => uniqueUserIds.Contains(u.UserId) && u.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
            
        _logger.LogInformation("[GetUserDisplayNamesAsync] Found {Count} users in database cache - Elapsed: {Elapsed}ms", 
            cachedUsers.Count, stopwatch.ElapsedMilliseconds);
        
        foreach (var cached in cachedUsers)
        {
            result[cached.UserId] = cached.DisplayName;
            // Also update memory cache
            _cache.Set($"clerk_user_{cached.UserId}", cached.DisplayName, MemoryCacheDuration);
        }
        
        // Find which users are not cached
        uncachedUserIds = uniqueUserIds.Except(result.Keys).ToList();
        
        if (uncachedUserIds.Any())
        {
            _logger.LogInformation("[GetUserDisplayNamesAsync] Fetching {Count} uncached users from Clerk API", uncachedUserIds.Count);
            
            // Fetch uncached users from Clerk API in parallel
            var tasks = uncachedUserIds.Select(async userId =>
            {
                var displayName = await GetUserDisplayNameAsync(userId);
                return new { UserId = userId, DisplayName = displayName };
            });

            var fetchedResults = await Task.WhenAll(tasks);
            
            foreach (var fetchedResult in fetchedResults)
            {
                result[fetchedResult.UserId] = fetchedResult.DisplayName;
            }
        }
        
        stopwatch.Stop();
        _logger.LogInformation("[GetUserDisplayNamesAsync] Complete - Total: {Total}ms, DB Cache Hits: {CacheHits}, API Calls: {ApiCalls}", 
            stopwatch.ElapsedMilliseconds, cachedUsers.Count, uncachedUserIds.Count);
        
        return result;
    }
}
