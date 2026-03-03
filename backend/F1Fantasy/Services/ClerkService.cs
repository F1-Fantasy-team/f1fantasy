using Clerk.BackendAPI;
using Clerk.BackendAPI.Models.Operations;
using Microsoft.Extensions.Caching.Memory;

namespace F1Fantasy.Services;

public class ClerkService
{
    private readonly ClerkBackendApi _clerkClient;
    private readonly ILogger<ClerkService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public ClerkService(ILogger<ClerkService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
        var clerkSecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY");
        
        if (string.IsNullOrEmpty(clerkSecretKey))
        {
            throw new InvalidOperationException("CLERK_SECRET_KEY environment variable is not set.");
        }

        _clerkClient = new ClerkBackendApi(clerkSecretKey);
    }

    /// <summary>
    /// Fetches user display name from Clerk. Returns first name + last name if available, otherwise username.
    /// Falls back to the user ID if all else fails. Results are cached for improved performance.
    /// </summary>
    public async Task<string> GetUserDisplayNameAsync(string userId)
    {
        // Try to get from cache first
        var cacheKey = $"clerk_user_{userId}";
        if (_cache.TryGetValue<string>(cacheKey, out var cachedName) && cachedName != null)
        {
            return cachedName;
        }

        try
        {
            var response = await _clerkClient.Users.GetAsync(userId);
            
            if (response?.User == null)
            {
                _logger.LogWarning("User {UserId} not found in Clerk", userId);
                var fallbackId = userId;
                _cache.Set(cacheKey, fallbackId, CacheDuration);
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
                _logger.LogWarning("No name or username found for user {UserId}, using ID as display name", userId);
                displayName = userId;
            }

            // Cache the result
            _cache.Set(cacheKey, displayName, CacheDuration);
            return displayName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Clerk", userId);
            var fallbackId = userId;
            // Cache failed lookups for a shorter time to retry sooner
            _cache.Set(cacheKey, fallbackId, TimeSpan.FromMinutes(5));
            return fallbackId;
        }
    }

    /// <summary>
    /// Fetches multiple user display names in parallel for efficiency
    /// </summary>
    public async Task<Dictionary<string, string>> GetUserDisplayNamesAsync(IEnumerable<string> userIds)
    {
        var uniqueUserIds = userIds.Distinct().ToList();
        var tasks = uniqueUserIds.Select(async userId => new
        {
            UserId = userId,
            DisplayName = await GetUserDisplayNameAsync(userId)
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.UserId, r => r.DisplayName);
    }
}
