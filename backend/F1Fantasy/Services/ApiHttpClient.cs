using System.Net;

namespace F1Fantasy.Services;

/// <summary>
/// HTTP client wrapper that handles rate limiting and retries for the Ergast F1 API
/// Implements exponential backoff when hitting rate limits (429 errors)
/// and adds polite delays between requests to avoid overwhelming the API
/// </summary>
public class ApiHttpClient
{
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 5;
    private const int InitialDelayMs = 500;
    private const int PoliteDelayMs = 100; // Delay between requests to be polite to the API
    private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;

    public ApiHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Makes an HTTP GET request with automatic retry on rate limit errors (429)
    /// Uses exponential backoff: 500ms, 1s, 2s, 4s, 8s
    /// Also enforces a polite delay between all requests to avoid hitting rate limits
    /// </summary>
    public async Task<string> GetStringWithRetryAsync(string url)
    {
        int attempt = 0;
        while (attempt < MaxRetries)
        {
            try
            {
                // Enforce polite delay between requests
                await _rateLimiter.WaitAsync();
                try
                {
                    var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                    if (timeSinceLastRequest.TotalMilliseconds < PoliteDelayMs)
                    {
                        var delayNeeded = PoliteDelayMs - (int)timeSinceLastRequest.TotalMilliseconds;
                        await Task.Delay(delayNeeded);
                    }
                    _lastRequestTime = DateTime.UtcNow;
                }
                finally
                {
                    _rateLimiter.Release();
                }

                var response = await _httpClient.GetAsync(url);

                // If we got rate limited, retry with exponential backoff
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    attempt++;
                    if (attempt >= MaxRetries)
                    {
                        response.EnsureSuccessStatusCode(); // Will throw on 429
                    }

                    // Exponential backoff: 500ms, 1s, 2s, 4s, 8s
                    var delayMs = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
                    Console.WriteLine($"Rate limit hit (429). Retrying in {delayMs}ms (attempt {attempt}/{MaxRetries})...");
                    await Task.Delay(delayMs);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                attempt++;
                if (attempt >= MaxRetries)
                {
                    throw;
                }

                var delayMs = InitialDelayMs * (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"Rate limit hit (429). Retrying in {delayMs}ms (attempt {attempt}/{MaxRetries})...");
                await Task.Delay(delayMs);
            }
        }

        throw new HttpRequestException("Max retries exceeded");
    }
}
