using F1Fantasy.Models;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("read")]
public class DriverStandingController : ControllerBase
{
    private readonly DriverStandingService _service;
    private readonly ILogger<DriverStandingController> _logger;

    public DriverStandingController(DriverStandingService service, ILogger<DriverStandingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<StandingsList>> GetDriverStandingsBySeason(string season)
    {
        _logger.LogInformation("GET request for driver standings: season {Season}", season);
        
        try
        {
            var standings = await _service.GetDriverStandingsBySeasonAsync(season);
            
            if (standings == null)
            {
                _logger.LogWarning("No driver standings found for season {Season}", season);
                return NotFound(new ErrorResponse
                {
                    Message = $"No driver standings found for season {season}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning {Count} driver standings for season {Season}, round {Round}", 
                standings.DriverStandings?.Count ?? 0, season, standings.Round);
            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver standings for season {Season}", season);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching driver standings",
                StatusCode = 500
            });
        }
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<StandingsList>> GetDriverStandingsByRound(string season, string round)
    {
        _logger.LogInformation("GET request for driver standings: season {Season}, round {Round}", 
            season, round);
        
        try
        {
            var standings = await _service.GetDriverStandingsByRoundAsync(season, round);
            
            if (standings == null)
            {
                _logger.LogWarning("No driver standings found for season {Season}, round {Round}", season, round);
                return NotFound(new ErrorResponse
                {
                    Message = $"No driver standings found for season {season}, round {round}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning {Count} driver standings for season {Season}, round {Round}", 
                standings.DriverStandings?.Count ?? 0, season, round);
            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver standings for season {Season}, round {Round}", 
                season, round);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching driver standings",
                StatusCode = 500
            });
        }
    }

    [HttpGet("season/{season}/round/{round}/driver/{driverId}")]
    public async Task<ActionResult<DriverStanding>> GetDriverStandingByDriver(string season, string round, string driverId)
    {
        _logger.LogInformation("GET request for driver standing: season {Season}, round {Round}, driver {DriverId}", 
            season, round, driverId);
        
        try
        {
            var standing = await _service.GetDriverStandingByDriverAsync(season, round, driverId);
            
            if (standing == null)
            {
                _logger.LogWarning("No standing found for driver {DriverId}, season {Season}, round {Round}", 
                    driverId, season, round);
                return NotFound(new ErrorResponse
                {
                    Message = $"No standing found for driver {driverId}, season {season}, round {round}",
                    StatusCode = 404
                });
            }

            _logger.LogDebug("Returning standing for driver {DriverId}: position {Position}", 
                driverId, standing.Position);
            return Ok(standing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching standing for driver {DriverId}, season {Season}, round {Round}", 
                driverId, season, round);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching driver standing",
                StatusCode = 500
            });
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<DriverStanding>>> GetCachedStandings()
    {
        _logger.LogInformation("GET request for all cached driver standings");
        
        try
        {
            var standings = await _service.GetCachedStandingsAsync();
            _logger.LogDebug("Returning {Count} cached driver standings", standings.Count());
            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached driver standings");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while fetching cached driver standings",
                StatusCode = 500
            });
        }
    }
}
