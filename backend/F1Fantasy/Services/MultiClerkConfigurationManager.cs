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
    private List<string>? _validIssuers;
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(24);
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

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
        // Return cached configuration if still valid (no lock needed for read)
        if (_cachedConfiguration != null && DateTime.UtcNow - _lastRefresh < _refreshInterval)
        {
            return _cachedConfiguration;
        }

        // Ensure only one refresh happens at a time
        await _refreshLock.WaitAsync(cancel);
        try
        {
            // Double-check after acquiring lock
            if (_cachedConfiguration != null && DateTime.UtcNow - _lastRefresh < _refreshInterval)
            {
                return _cachedConfiguration;
            }

            // Fetch configurations from all Clerk instances in parallel
            var configurations = new List<OpenIdConnectConfiguration>();
            var issuers = new List<string>();

            var fetchTasks = _metadataAddresses.Select(async metadataAddress =>
            {
                try
                {
                    _logger.LogInformation("Fetching OIDC configuration from: {MetadataAddress}", metadataAddress);

                    var documentRetriever = new HttpDocumentRetriever(_httpClient)
                    {
                        RequireHttps = metadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    };

                    var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress,
                        new OpenIdConnectConfigurationRetriever(),
                        documentRetriever);

                    var config = await configurationManager.GetConfigurationAsync(cancel);

                    _logger.LogInformation("Successfully fetched configuration from: {MetadataAddress} with {KeyCount} signing keys and issuer: {Issuer}", 
                        metadataAddress, config.SigningKeys.Count, config.Issuer);
                    
                    return (config, metadataAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch configuration from: {MetadataAddress}", metadataAddress);
                    return (null, metadataAddress);
                }
            });

            var results = await Task.WhenAll(fetchTasks);

            foreach (var (config, metadataAddress) in results)
            {
                if (config != null)
                {
                    configurations.Add(config);
                    
                    // Collect issuers from discovery documents and normalize (remove trailing slash)
                    if (!string.IsNullOrEmpty(config.Issuer))
                    {
                        var normalizedIssuer = config.Issuer.TrimEnd('/');
                        if (!issuers.Contains(normalizedIssuer, StringComparer.Ordinal))
                        {
                            issuers.Add(normalizedIssuer);
                        }
                    }
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

            // Track seen keys by stable identifiers to avoid duplicates across instances.
            // Prefer JWK thumbprint when available, fall back to KeyId.
            var seenThumbprints = new HashSet<string>(StringComparer.Ordinal);
            var seenKeyIds = new HashSet<string>(StringComparer.Ordinal);

            // Merge signing keys from ALL configurations
            foreach (var config in configurations)
            {
                foreach (var key in config.SigningKeys)
                {
                    string? thumbprint = null;
                    if (key is JsonWebKey jwk)
                    {
                        // ComputeJwkThumbprint returns a byte array, convert to base64 string.
                        var thumbprintBytes = jwk.ComputeJwkThumbprint();
                        thumbprint = Convert.ToBase64String(thumbprintBytes);
                    }

                    if (!string.IsNullOrEmpty(thumbprint))
                    {
                        // De-duplicate by thumbprint when available.
                        if (seenThumbprints.Add(thumbprint))
                        {
                            mergedConfig.SigningKeys.Add(key);
                        }
                    }
                    else if (!string.IsNullOrEmpty(key.KeyId))
                    {
                        // Fall back to KeyId if no thumbprint is available.
                        if (seenKeyIds.Add(key.KeyId))
                        {
                            mergedConfig.SigningKeys.Add(key);
                        }
                    }
                    else
                    {
                        // As a last resort, fall back to reference-based Contains to avoid changing
                        // behavior for keys without any stable identifier.
                        if (!mergedConfig.SigningKeys.Contains(key))
                        {
                            mergedConfig.SigningKeys.Add(key);
                        }
                    }
                }

                _logger.LogInformation("Processed {KeyCount} signing keys from issuer: {Issuer}",
                    config.SigningKeys.Count, config.Issuer);
            }

            _logger.LogInformation("Merged configuration contains {TotalKeys} unique signing keys",
                mergedConfig.SigningKeys.Count);

            _cachedConfiguration = mergedConfig;
            _validIssuers = issuers;
            _lastRefresh = DateTime.UtcNow;

            return mergedConfig;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void RequestRefresh()
    {
        _cachedConfiguration = null;
        _validIssuers = null;
        _lastRefresh = DateTime.MinValue;
    }

    /// <summary>
    /// Gets the list of valid issuers derived from the fetched discovery documents.
    /// Returns null if configuration has not been fetched yet.
    /// </summary>
    public IReadOnlyList<string>? GetValidIssuers() => _validIssuers?.AsReadOnly();
}
