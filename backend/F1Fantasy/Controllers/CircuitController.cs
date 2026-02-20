using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CircuitController : ControllerBase
{
    private readonly CircuitService _circuitService;

    public CircuitController(CircuitService circuitService)
    {
        _circuitService = circuitService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Circuit>>> GetAllCircuits()
    {
        var circuits = await _circuitService.GetAllCircuitsAsync();
        return Ok(circuits);
    }

    [HttpGet("{circuitId}")]
    public async Task<ActionResult<Circuit>> GetCircuitById(string circuitId)
    {
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
        var circuits = _circuitService.GetCachedCircuits();
        return Ok(circuits);
    }
}
