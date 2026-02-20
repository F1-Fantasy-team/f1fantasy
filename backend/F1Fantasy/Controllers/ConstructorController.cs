using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConstructorController : ControllerBase
{
    private readonly ConstructorService _constructorService;
    private readonly ILogger<ConstructorController> _logger;

    public ConstructorController(ConstructorService constructorService, ILogger<ConstructorController> logger)
    {
        _constructorService = constructorService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Constructor>>> GetAllConstructors()
    {
        _logger.LogInformation("GET /api/constructor - Fetching all constructors");
        var constructors = await _constructorService.GetAllConstructorsAsync();
        return Ok(constructors);
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<Constructor>>> GetConstructorsBySeason(string season)
    {
        _logger.LogInformation("GET /api/constructor/season/{Season} - Fetching constructors for season", season);
        var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);
        return Ok(constructors);
    }

    [HttpGet("{constructorId}")]
    public async Task<ActionResult<Constructor>> GetConstructorById(string constructorId)
    {
        _logger.LogInformation("GET /api/constructor/{ConstructorId} - Fetching constructor by ID", constructorId);
        var constructor = await _constructorService.GetConstructorByIdAsync(constructorId);
        if (constructor == null)
        {
            return NotFound();
        }
        return Ok(constructor);
    }

    [HttpGet("cached")]
    public ActionResult<IEnumerable<Constructor>> GetCachedConstructors()
    {
        _logger.LogInformation("GET /api/constructor/cached - Fetching cached constructors");
        var constructors = _constructorService.GetCachedConstructors();
        return Ok(constructors);
    }
}
