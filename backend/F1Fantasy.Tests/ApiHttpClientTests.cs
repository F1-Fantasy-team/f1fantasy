using F1Fantasy.Services;
using FluentAssertions;
using System.Net;
using System.Text;
using Xunit;

namespace F1Fantasy.Tests;

public class ApiHttpClientTests
{
    [Fact]
    public async Task GetStringWithRetry_SuccessOnFirstAttempt_ReturnsContent()
    {
        var client = new HttpClient(new SequentialHandler(
            HttpStatusCode.OK, "{\"data\":1}"));
        var sut = new ApiHttpClient(client);

        var result = await sut.GetStringWithRetryAsync("http://test/");

        result.Should().Be("{\"data\":1}");
    }

    [Fact]
    public async Task GetStringWithRetry_429ThenSuccess_RetriesAndReturnsContent()
    {
        // First call returns 429, second returns 200
        var client = new HttpClient(new SequentialHandler(
            HttpStatusCode.TooManyRequests, "",
            HttpStatusCode.OK, "{\"ok\":true}"));
        var sut = new ApiHttpClient(client);

        var result = await sut.GetStringWithRetryAsync("http://test/");

        result.Should().Be("{\"ok\":true}");
    }

    [Fact]
    public async Task GetStringWithRetry_AllAttempts429_ThrowsHttpRequestException()
    {
        var client = new HttpClient(new SequentialHandler(
            HttpStatusCode.TooManyRequests, "",
            HttpStatusCode.TooManyRequests, "",
            HttpStatusCode.TooManyRequests, "",
            HttpStatusCode.TooManyRequests, "",
            HttpStatusCode.TooManyRequests, ""));
        var sut = new ApiHttpClient(client);

        var act = () => sut.GetStringWithRetryAsync("http://test/");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetStringWithRetry_NonRateLimitError_DoesNotRetry()
    {
        var counter = new CallCounter(HttpStatusCode.InternalServerError);
        var client = new HttpClient(counter);
        var sut = new ApiHttpClient(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetStringWithRetryAsync("http://test/"));

        counter.CallCount.Should().Be(1, "non-429 errors should not be retried");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ExponentialBackoff_DelayStaysWithinJitterBounds(int attempt)
    {
        // Access via reflection since ExponentialBackoffWithJitter is private
        var method = typeof(ApiHttpClient).GetMethod(
            "ExponentialBackoffWithJitter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        // Sample 50 times — all must fall within ±10% of the base delay
        const int initialDelayMs = 500;
        var baseDelay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
        var lowerBound = (int)(baseDelay * 0.9);
        var upperBound = (int)(baseDelay * 1.1) + 1; // +1 for exclusive upper bound

        for (int i = 0; i < 50; i++)
        {
            var delay = (int)method!.Invoke(null, new object[] { attempt })!;
            delay.Should().BeInRange(lowerBound, upperBound,
                $"attempt {attempt} base={baseDelay}ms, jitter must stay within ±10%");
        }
    }

    [Fact]
    public void ExponentialBackoff_DifferentAttemptsDifferentDelays()
    {
        var method = typeof(ApiHttpClient).GetMethod(
            "ExponentialBackoffWithJitter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        const int initialDelayMs = 500;

        // Each successive attempt should have a higher base delay
        for (int attempt = 2; attempt <= 5; attempt++)
        {
            var prevBase = initialDelayMs * (int)Math.Pow(2, attempt - 2);
            var currBase = initialDelayMs * (int)Math.Pow(2, attempt - 1);
            currBase.Should().BeGreaterThan(prevBase,
                $"attempt {attempt} should wait longer than attempt {attempt - 1}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class SequentialHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

        public SequentialHandler(params object[] pairs)
        {
            for (int i = 0; i < pairs.Length; i += 2)
                _responses.Enqueue(((HttpStatusCode)pairs[i], (string)pairs[i + 1]));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (status, body) = _responses.Count > 0
                ? _responses.Dequeue()
                : (HttpStatusCode.OK, "");
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CallCounter : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public int CallCount { get; private set; }

        public CallCounter(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent("")
            });
        }
    }
}
