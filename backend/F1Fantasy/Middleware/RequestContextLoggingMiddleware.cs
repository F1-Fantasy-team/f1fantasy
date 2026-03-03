using System.Diagnostics;

namespace F1Fantasy.Middleware;

public class RequestContextLoggingMiddleware
{
    private const string RequestIdHeaderName = "X-Request-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextLoggingMiddleware> _logger;

    public RequestContextLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestContextLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        context.TraceIdentifier = requestId;
        context.Items[RequestIdHeaderName] = requestId;
        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.ToString()
        }))
        {
            _logger.LogInformation("Request started");

            try
            {
                await _next(context);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Request completed with status {StatusCode} in {ElapsedMilliseconds} ms",
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                _logger.LogError(
                    "Request failed after {ElapsedMilliseconds} ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}