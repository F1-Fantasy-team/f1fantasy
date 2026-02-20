using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public ActionResult<IEnumerable<Circuit>> GetCachedCircuits()
    {
        _logger.LogInformation("GET /api/circuit/cached - Fetching cached circuits");
        var circuits = _circuitService.GetCachedCircuits();
        return Ok(circuits);
    }
}
