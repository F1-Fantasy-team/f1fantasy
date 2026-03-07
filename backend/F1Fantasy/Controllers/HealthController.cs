using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using F1Fantasy.Data;
using Microsoft.EntityFrameworkCore;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
[DisableRateLimiting] // Health checks shouldn't be rate limited
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly F1FantasyDbContext _dbContext;

    public HealthController(ILogger<HealthController> logger, F1FantasyDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Health check endpoint for monitoring services (Render.com, uptime monitors, etc.)
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 10)] // Cache for 10 seconds to reduce load
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "F1Fantasy API"
        });
    }

    /// <summary>
    /// Lightweight ping endpoint for keepalive
    /// </summary>
    [HttpGet("ping")]
    [ResponseCache(Duration = 5)]
    public IActionResult Ping()
    {
        return Ok("pong");
    }

    /// <summary>
    /// Health check with database connectivity check - keeps DB connection pool warm
    /// Use this for uptime monitoring services (Betterstack, UptimeRobot, etc.)
    /// </summary>
    [HttpGet("ready")]
    [ResponseCache(Duration = 30)] // Cache for 30 seconds to avoid hammering DB
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            // Execute a lightweight query to keep DB connection pool warm
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                _logger.LogWarning("Database connection failed during health check");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    database = "disconnected",
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "ready",
                database = "connected",
                timestamp = DateTime.UtcNow,
                service = "F1Fantasy API"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed: {Message}", ex.Message);
            return StatusCode(503, new
            {
                status = "unhealthy",
                error = "Database connectivity check failed",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
