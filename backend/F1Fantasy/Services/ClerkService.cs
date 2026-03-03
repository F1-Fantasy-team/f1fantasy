using Clerk.BackendAPI;
using Clerk.BackendAPI.Models.Operations;

namespace F1Fantasy.Services;

public class ClerkService
{
    private readonly ClerkBackendApi _clerkClient;
    private readonly ILogger<ClerkService> _logger;

    public ClerkService(ILogger<ClerkService> logger)
    {
        _logger = logger;
        var clerkSecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY");
        
        if (string.IsNullOrEmpty(clerkSecretKey))
        {
            throw new InvalidOperationException("CLERK_SECRET_KEY environment variable is not set.");
        }

        _clerkClient = new ClerkBackendApi(clerkSecretKey);
    }

    /// <summary>
    /// Fetches user display name from Clerk. Returns first name + last name if available, otherwise username.
    /// Falls back to the user ID if all else fails.
    /// </summary>
    public async Task<string> GetUserDisplayNameAsync(string userId)
    {
        try
        {
            var response = await _clerkClient.Users.GetAsync(userId);
            
            if (response?.User == null)
            {
                _logger.LogWarning("User {UserId} not found in Clerk", userId);
                return userId; // Fallback to user ID
            }

            var user = response.User;

            // Try to build full name from first and last name
            var firstName = user.FirstName?.Trim();
            var lastName = user.LastName?.Trim();

            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            {
                return $"{firstName} {lastName}";
            }
            else if (!string.IsNullOrEmpty(firstName))
            {
                return firstName;
            }
            else if (!string.IsNullOrEmpty(lastName))
            {
                return lastName;
            }

            // Fallback to username
            if (!string.IsNullOrEmpty(user.Username))
            {
                return user.Username;
            }

            // Last resort - use the user ID
            _logger.LogWarning("No name or username found for user {UserId}, using ID as display name", userId);
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId} from Clerk", userId);
            return userId; // Fallback to user ID on error
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
