using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RaceController : ControllerBase
{
    private readonly RaceService _raceService;

    public RaceController(RaceService raceService)
    {
        _raceService = raceService;
    }

    [HttpGet("{season}")]
    public async Task<ActionResult<IEnumerable<Race>>> GetRacesBySeason(string season)
    {
        var races = await _raceService.GetRacesForSeasonAsync(season);
        return Ok(races);
    }

    [HttpGet("{season}/{round}")]
    public async Task<ActionResult<Race>> GetRaceByRound(string season, string round)
    {
        var race = await _raceService.GetRaceByRoundAsync(season, round);
        if (race == null)
        {
            return NotFound();
        }
        return Ok(race);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Race>>> GetAllRaces()
    {
        var races = await _raceService.GetAllRacesAsync();
        return Ok(races);
    }
}
