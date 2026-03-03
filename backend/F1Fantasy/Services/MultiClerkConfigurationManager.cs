using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace F1Fantasy.Services;

/// <summary>
/// Custom configuration manager that fetches and merges OIDC configuration 
/// (signing keys) from multiple Clerk instances.
/// This allows accepting JWT tokens from both production and development Clerk environments.
/// </summary>
public class MultiClerkConfigurationManager : IConfigurationManager<OpenIdConnectConfiguration>
{
    private readonly List<string> _metadataAddresses;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MultiClerkConfigurationManager> _logger;
    private OpenIdConnectConfiguration? _cachedConfiguration;
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(24);

    public MultiClerkConfigurationManager(
        IEnumerable<string> clerkUrls, 
        HttpClient httpClient,
        ILogger<MultiClerkConfigurationManager> logger)
    {
        _metadataAddresses = clerkUrls
            .Select(url => url.TrimEnd('/') + "/.well-known/openid-configuration")
            .ToList();
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
    {
        // Return cached configuration if still valid
        if (_cachedConfiguration != null && DateTime.UtcNow - _lastRefresh < _refreshInterval)
        {
            return _cachedConfiguration;
        }

        // Fetch configurations from all Clerk instances
        var configurations = new List<OpenIdConnectConfiguration>();
        
        foreach (var metadataAddress in _metadataAddresses)
        {
            try
            {
                _logger.LogInformation("Fetching OIDC configuration from: {MetadataAddress}", metadataAddress);
                
                var response = await _httpClient.GetStringAsync(metadataAddress, cancel);
                var config = OpenIdConnectConfiguration.Create(response);
                
                configurations.Add(config);
                _logger.LogInformation("Successfully fetched configuration from: {MetadataAddress}", metadataAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch configuration from: {MetadataAddress}", metadataAddress);
                // Continue with other endpoints even if one fails
            }
        }

        if (configurations.Count == 0)
        {
            throw new InvalidOperationException("Failed to fetch configuration from any Clerk instance");
        }

        // Merge configurations - combine signing keys from all instances
        var mergedConfig = new OpenIdConnectConfiguration
        {
            Issuer = configurations[0].Issuer, // Use first as primary
            AuthorizationEndpoint = configurations[0].AuthorizationEndpoint,
            TokenEndpoint = configurations[0].TokenEndpoint,
            UserInfoEndpoint = configurations[0].UserInfoEndpoint,
            JwksUri = configurations[0].JwksUri
        };

        // Merge signing keys from ALL configurations
        foreach (var config in configurations)
        {
            foreach (var key in config.SigningKeys)
            {
                if (!mergedConfig.SigningKeys.Contains(key))
                {
                    mergedConfig.SigningKeys.Add(key);
                }
            }
            
            _logger.LogInformation("Added {KeyCount} signing keys from issuer: {Issuer}", 
                config.SigningKeys.Count, config.Issuer);
        }

        _logger.LogInformation("Merged configuration contains {TotalKeys} total signing keys", 
            mergedConfig.SigningKeys.Count);

        _cachedConfiguration = mergedConfig;
        _lastRefresh = DateTime.UtcNow;

        return mergedConfig;
    }

    public void RequestRefresh()
    {
        _cachedConfiguration = null;
        _lastRefresh = DateTime.MinValue;
    }
}
