using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace F1Fantasy.Validation;

/// <summary>
/// Input validation helpers to complement EF Core's SQL injection protection
/// </summary>
public static class ValidationExtensions
{
    // Maximum lengths to prevent database errors and DoS attacks
    public const int MAX_GROUP_NAME_LENGTH = 100;
    public const int MAX_WILDCARD_REASON_LENGTH = 500;
    public const int MAX_CONSTRUCTOR_LIST_SIZE = 20; // Reasonable upper bound
    public const int MAX_DRIVER_LIST_SIZE = 30;

    /// <summary>
    /// Validates group name is safe and within limits
    /// </summary>
    public static void ValidateGroupName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Group name cannot be empty");
        }

        if (name.Length > MAX_GROUP_NAME_LENGTH)
        {
            throw new ArgumentException($"Group name cannot exceed {MAX_GROUP_NAME_LENGTH} characters");
        }

        // Prevent XSS attempts (basic check - output encoding is primary defense)
        if (name.Contains('<') || name.Contains('>'))
        {
            throw new ArgumentException("Group name contains invalid characters");
        }
    }

    /// <summary>
    /// Validates wildcard reason text
    /// </summary>
    public static void ValidateWildcardReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return; // Optional field
        }

        if (reason.Length > MAX_WILDCARD_REASON_LENGTH)
        {
            throw new ArgumentException($"Wildcard reason cannot exceed {MAX_WILDCARD_REASON_LENGTH} characters");
        }
    }

    /// <summary>
    /// Validates list size to prevent DoS attacks with massive arrays
    /// </summary>
    public static void ValidateListSize<T>(List<T> list, int maxSize, string itemName)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (list.Count > maxSize)
        {
            throw new ArgumentException($"Cannot submit more than {maxSize} {itemName}");
        }
    }

    /// <summary>
    /// Validates ID format (alphanumeric, underscores, hyphens only)
    /// Prevents injection attempts in string-based IDs
    /// </summary>
    public static void ValidateId(string id, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"{fieldName} cannot be empty");
        }

        // F1 API uses IDs like "max_verstappen", "red_bull", "monaco"
        if (!Regex.IsMatch(id, @"^[a-z0-9_-]+$"))
        {
            throw new ArgumentException($"{fieldName} contains invalid characters. Use only lowercase letters, numbers, underscores, and hyphens.");
        }
    }

    /// <summary>
    /// Validates season format (4-digit year)
    /// </summary>
    public static void ValidateSeason(string season)
    {
        if (!Regex.IsMatch(season, @"^\d{4}$"))
        {
            throw new ArgumentException("Season must be a 4-digit year (e.g., '2023')");
        }

        var year = int.Parse(season);
        if (year < 1950 || year > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentException($"Season must be between 1950 and {DateTime.UtcNow.Year + 1}");
        }
    }

    /// <summary>
    /// Validates lock mode is a valid enum value
    /// </summary>
    public static void ValidateLockMode(string lockMode)
    {
        var validModes = new[] { "admin", "system", "hybrid" };
        if (!validModes.Contains(lockMode.ToLower()))
        {
            throw new ArgumentException($"Lock mode must be one of: {string.Join(", ", validModes)}");
        }
    }
}
