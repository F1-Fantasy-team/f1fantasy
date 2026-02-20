using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConstructorController : ControllerBase
{
    private readonly ConstructorService _constructorService;

    public ConstructorController(ConstructorService constructorService)
    {
        _constructorService = constructorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Constructor>>> GetAllConstructors()
    {
        var constructors = await _constructorService.GetAllConstructorsAsync();
        return Ok(constructors);
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<Constructor>>> GetConstructorsBySeason(string season)
    {
        var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);
        return Ok(constructors);
    }

    [HttpGet("{constructorId}")]
    public async Task<ActionResult<Constructor>> GetConstructorById(string constructorId)
    {
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
        var constructors = _constructorService.GetCachedConstructors();
        return Ok(constructors);
    }
}
