using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceController : ControllerBase
{
    private readonly RaceService _raceService;
    private readonly ILogger<RaceController> _logger;

    public RaceController(RaceService raceService, ILogger<RaceController> logger)
    {
        _raceService = raceService;
        _logger = logger;
    }

    [HttpGet("{season}")]
    public async Task<ActionResult<IEnumerable<Race>>> GetRacesBySeason(string season)
    {
        try
        {
            _logger.LogInformation("GET /api/race/{Season} - Fetching races for season", season);
            var races = await _raceService.GetRacesForSeasonAsync(season);
            _logger.LogInformation("Successfully retrieved {Count} races for season {Season}", races.Count(), season);
            return Ok(races);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching races for season {Season}", season);
            throw;
        }
    }

    [HttpGet("{season}/{round}")]
    public async Task<ActionResult<Race>> GetRaceByRound(string season, string round)
    {
        try
        {
            _logger.LogInformation("GET /api/race/{Season}/{Round} - Fetching race by round", season, round);
            var race = await _raceService.GetRaceByRoundAsync(season, round);
            if (race == null)
            {
                _logger.LogWarning("Race not found for season {Season}, round {Round}", season, round);
                return NotFound(new { message = $"Race not found for season {season}, round {round}" });
            }
            _logger.LogInformation("Successfully retrieved race for season {Season}, round {Round}", season, round);
            return Ok(race);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching race for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Race>>> GetAllRaces()
    {
        try
        {
            _logger.LogInformation("GET /api/race - Fetching all races");
            var races = await _raceService.GetAllRacesAsync();
            _logger.LogInformation("Successfully retrieved {Count} total races", races.Count());
            return Ok(races);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all races");
            throw;
        }
    }
}
