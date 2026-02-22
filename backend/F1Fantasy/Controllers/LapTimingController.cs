using F1Fantasy.Models;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LapTimingController : ControllerBase
{
    private readonly LapTimingService _service;
    private readonly ILogger<LapTimingController> _logger;

    public LapTimingController(LapTimingService service, ILogger<LapTimingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<RaceWithLaps>> GetLapsByRace(string season, string round)
    {
        _logger.LogInformation("GET request for lap timings: season {Season}, round {Round}", season, round);
        
        try
        {
            var laps = await _service.GetLapsByRaceAsync(season, round);
            
            if (laps == null)
            {
                _logger.LogWarning("No lap timings found for season {Season}, round {Round}", season, round);
                return NotFound(new ErrorResponse
                {
                    Message = $"No lap timings found for season {season}, round {round}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning {LapCount} laps for season {Season}, round {Round}", 
                laps.Laps?.Count ?? 0, season, round);
            return Ok(laps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lap timings for season {Season}, round {Round}", season, round);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching lap timings",
                StatusCode = 500
            });
        }
    }

    [HttpGet("season/{season}/round/{round}/lap/{lapNumber}")]
    public async Task<ActionResult<Lap>> GetLapByNumber(string season, string round, string lapNumber)
    {
        _logger.LogInformation("GET request for lap {LapNumber}: season {Season}, round {Round}", 
            lapNumber, season, round);
        
        try
        {
            var lap = await _service.GetLapByNumberAsync(season, round, lapNumber);
            
            if (lap == null)
            {
                _logger.LogWarning("No timings found for lap {LapNumber}, season {Season}, round {Round}", 
                    lapNumber, season, round);
                return NotFound(new ErrorResponse
                {
                    Message = $"No timings found for lap {lapNumber}, season {season}, round {round}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning {Count} timings for lap {LapNumber}", 
                lap.Timings?.Count ?? 0, lapNumber);
            return Ok(lap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lap {LapNumber} for season {Season}, round {Round}", 
                lapNumber, season, round);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching lap timings",
                StatusCode = 500
            });
        }
    }

    [HttpGet("season/{season}/round/{round}/driver/{driverId}")]
    public async Task<ActionResult<List<LapTiming>>> GetLapsByDriver(string season, string round, string driverId)
    {
        _logger.LogInformation("GET request for driver {DriverId} laps: season {Season}, round {Round}", 
            driverId, season, round);
        
        try
        {
            var laps = await _service.GetLapsByDriverAsync(season, round, driverId);
            
            if (laps == null || !laps.Any())
            {
                _logger.LogWarning("No laps found for driver {DriverId}, season {Season}, round {Round}", 
                    driverId, season, round);
                return NotFound(new ErrorResponse
                {
                    Message = $"No laps found for driver {driverId}, season {season}, round {round}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning {Count} laps for driver {DriverId}", laps.Count, driverId);
            return Ok(laps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching laps for driver {DriverId}, season {Season}, round {Round}", 
                driverId, season, round);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching driver lap timings",
                StatusCode = 500
            });
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<LapTiming>>> GetCachedLaps()
    {
        _logger.LogInformation("GET request for all cached lap timings");
        
        try
        {
            var laps = await _service.GetCachedLapsAsync();
            _logger.LogDebug("Returning {Count} cached lap timings", laps.Count());
            return Ok(laps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached lap timings");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching cached lap timings",
                StatusCode = 500
            });
        }
    }
}
