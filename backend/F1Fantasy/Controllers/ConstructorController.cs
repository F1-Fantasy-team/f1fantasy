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
    public async Task<ActionResult<IEnumerable<Constructor>>> GetCachedConstructors()
    {
        try
        {
            _logger.LogInformation("GET /api/constructor/cached - Fetching cached constructors");
            var constructors = await _constructorService.GetCachedConstructorsAsync();
            
            if (!constructors.Any())
            {
                _logger.LogWarning("No cached constructors found. Database may be empty.");
                return Ok(new { 
                    message = "No cached constructors found. Try calling /api/constructor first to populate the cache.",
                    constructors = constructors 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached constructors", constructors.Count());
            return Ok(constructors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached constructors");
            throw;
        }
    }
}
