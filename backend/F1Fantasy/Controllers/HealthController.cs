using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
[DisableRateLimiting] // Health checks shouldn't be rate limited
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
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
}
