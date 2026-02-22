using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("read")]
public class CircuitController : ControllerBase
{
    private readonly CircuitService _circuitService;
    private readonly ILogger<CircuitController> _logger;

    public CircuitController(CircuitService circuitService, ILogger<CircuitController> logger)
    {
        _circuitService = circuitService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Circuit>>> GetAllCircuits()
    {
        _logger.LogInformation("GET /api/circuit - Fetching all circuits");
        var circuits = await _circuitService.GetAllCircuitsAsync();
        return Ok(circuits);
    }

    [HttpGet("{circuitId}")]
    public async Task<ActionResult<Circuit>> GetCircuitById(string circuitId)
    {
        _logger.LogInformation("GET /api/circuit/{CircuitId} - Fetching circuit by ID", circuitId);
        var circuit = await _circuitService.GetCircuitByIdAsync(circuitId);
        if (circuit == null)
        {
            return NotFound();
        }
        return Ok(circuit);
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<Circuit>>> GetCachedCircuits()
    {
        try
        {
            _logger.LogInformation("GET /api/circuit/cached - Fetching cached circuits");
            var circuits = await _circuitService.GetCachedCircuitsAsync();
            
            if (!circuits.Any())
            {
                _logger.LogWarning("No cached circuits found. Database may be empty.");
                return Ok(new { 
                    message = "No cached circuits found. Try calling /api/circuit first to populate the cache.",
                    circuits = circuits 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached circuits", circuits.Count());
            return Ok(circuits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached circuits");
            throw;
        }
    }
}
