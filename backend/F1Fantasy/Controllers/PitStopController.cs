using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PitStopController : ControllerBase
{
    private readonly PitStopService _pitStopService;
    private readonly ILogger<PitStopController> _logger;

    public PitStopController(PitStopService pitStopService, ILogger<PitStopController> logger)
    {
        _pitStopService = pitStopService;
        _logger = logger;
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<RaceWithPitStops>> GetPitStopsByRace(string season, string round)
    {
        try
        {
            _logger.LogInformation("GET /api/pitstop/season/{Season}/round/{Round} - Fetching pit stops for race", 
                season, round);
            var pitStops = await _pitStopService.GetPitStopsByRaceAsync(season, round);
            
            if (pitStops == null)
            {
                _logger.LogWarning("No pit stops found for season {Season}, round {Round}", season, round);
                return NotFound(new { message = $"No pit stops found for season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} pit stops for season {Season}, round {Round}", 
                pitStops.PitStops?.Count ?? 0, season, round);
            return Ok(pitStops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pit stops for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}/driver/{driverId}")]
    public async Task<ActionResult<IEnumerable<PitStop>>> GetPitStopsByDriver(string season, string round, string driverId)
    {
        try
        {
            _logger.LogInformation("GET /api/pitstop/season/{Season}/round/{Round}/driver/{DriverId} - Fetching driver pit stops", 
                season, round, driverId);
            var pitStops = await _pitStopService.GetPitStopsByDriverAsync(season, round, driverId);
            
            if (!pitStops.Any())
            {
                _logger.LogWarning("No pit stops found for season {Season}, round {Round}, driver {DriverId}", 
                    season, round, driverId);
                return NotFound(new { message = $"No pit stops found for driver {driverId} in season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} pit stops for driver {DriverId}", 
                pitStops.Count(), driverId);
            return Ok(pitStops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pit stops for season {Season}, round {Round}, driver {DriverId}", 
                season, round, driverId);
            throw;
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<PitStop>>> GetCachedPitStops()
    {
        try
        {
            _logger.LogInformation("GET /api/pitstop/cached - Fetching cached pit stops");
            var pitStops = await _pitStopService.GetCachedPitStopsAsync();
            
            if (!pitStops.Any())
            {
                _logger.LogWarning("No cached pit stops found. Database may be empty.");
                return Ok(new { 
                    message = "No cached pit stops found. Try calling /api/pitstop/season/2025/round/1 first to populate the cache.",
                    pitStops = pitStops 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached pit stops", pitStops.Count());
            return Ok(pitStops);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached pit stops");
            throw;
        }
    }
}
