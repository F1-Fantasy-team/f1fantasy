using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeasonController : ControllerBase
{
    private readonly SeasonService _seasonService;
    private readonly ILogger<SeasonController> _logger;

    public SeasonController(SeasonService seasonService, ILogger<SeasonController> logger)
    {
        _seasonService = seasonService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Season>>> GetAllSeasons()
    {
        _logger.LogInformation("GET /api/season - Fetching all seasons");
        var seasons = await _seasonService.GetAllSeasonsAsync();
        return Ok(seasons);
    }

    [HttpGet("{year}")]
    public async Task<ActionResult<Season>> GetSeasonByYear(string year)
    {
        _logger.LogInformation("GET /api/season/{Year} - Fetching season by year", year);
        var season = await _seasonService.GetSeasonByYearAsync(year);
        if (season == null)
        {
            return NotFound();
        }
        return Ok(season);
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<Season>>> GetCachedSeasons()
    {
        try
        {
            _logger.LogInformation("GET /api/season/cached - Fetching cached seasons");
            var seasons = await _seasonService.GetCachedSeasonsAsync();
            
            if (!seasons.Any())
            {
                _logger.LogWarning("No cached seasons found. Database may be empty.");
                return Ok(new { 
                    message = "No cached seasons found. Try calling /api/season first to populate the cache.",
                    seasons = seasons 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached seasons", seasons.Count());
            return Ok(seasons);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached seasons");
            throw;
        }
    }
}
