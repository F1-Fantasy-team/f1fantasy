using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeasonController : ControllerBase
{
    private readonly SeasonService _seasonService;

    public SeasonController(SeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Season>>> GetAllSeasons()
    {
        var seasons = await _seasonService.GetAllSeasonsAsync();
        return Ok(seasons);
    }

    [HttpGet("{year}")]
    public async Task<ActionResult<Season>> GetSeasonByYear(string year)
    {
        var season = await _seasonService.GetSeasonByYearAsync(year);
        if (season == null)
        {
            return NotFound();
        }
        return Ok(season);
    }

    [HttpGet("cached")]
    public ActionResult<IEnumerable<Season>> GetCachedSeasons()
    {
        var seasons = _seasonService.GetCachedSeasons();
        return Ok(seasons);
    }
}
