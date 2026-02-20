using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualifyingController : ControllerBase
{
    private readonly QualifyingService _qualifyingService;
    private readonly ILogger<QualifyingController> _logger;

    public QualifyingController(QualifyingService qualifyingService, ILogger<QualifyingController> logger)
    {
        _qualifyingService = qualifyingService;
        _logger = logger;
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<RaceWithQualifying>>> GetQualifyingBySeason(string season)
    {
        try
        {
            _logger.LogInformation("GET /api/qualifying/season/{Season} - Fetching qualifying for season", season);
            var qualifying = await _qualifyingService.GetQualifyingBySeasonAsync(season);
            _logger.LogInformation("Successfully retrieved qualifying for {Count} races in season {Season}", 
                qualifying.Count(), season);
            return Ok(qualifying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching qualifying for season {Season}", season);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<RaceWithQualifying>> GetQualifyingByRace(string season, string round)
    {
        try
        {
            _logger.LogInformation("GET /api/qualifying/season/{Season}/round/{Round} - Fetching qualifying for race", 
                season, round);
            var qualifying = await _qualifyingService.GetQualifyingByRaceAsync(season, round);
            
            if (qualifying == null)
            {
                _logger.LogWarning("No qualifying found for season {Season}, round {Round}", season, round);
                return NotFound(new { message = $"No qualifying found for season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} qualifying results for season {Season}, round {Round}", 
                qualifying.QualifyingResults?.Count ?? 0, season, round);
            return Ok(qualifying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching qualifying for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}/driver/{driverId}")]
    public async Task<ActionResult<Qualifying>> GetQualifyingByDriver(string season, string round, string driverId)
    {
        try
        {
            _logger.LogInformation("GET /api/qualifying/season/{Season}/round/{Round}/driver/{DriverId} - Fetching driver qualifying", 
                season, round, driverId);
            var qualifying = await _qualifyingService.GetQualifyingByDriverAsync(season, round, driverId);
            
            if (qualifying == null)
            {
                _logger.LogWarning("No qualifying found for season {Season}, round {Round}, driver {DriverId}", 
                    season, round, driverId);
                return NotFound(new { message = $"No qualifying found for driver {driverId} in season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved qualifying for driver {DriverId}, position {Position}", 
                driverId, qualifying.Position);
            return Ok(qualifying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching qualifying for season {Season}, round {Round}, driver {DriverId}", 
                season, round, driverId);
            throw;
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<Qualifying>>> GetCachedQualifying()
    {
        try
        {
            _logger.LogInformation("GET /api/qualifying/cached - Fetching cached qualifying");
            var qualifying = await _qualifyingService.GetCachedQualifyingAsync();
            
            if (!qualifying.Any())
            {
                _logger.LogWarning("No cached qualifying found. Database may be empty.");
                return Ok(new { 
                    message = "No cached qualifying found. Try calling /api/qualifying/season/2025 first to populate the cache.",
                    qualifying = qualifying 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached qualifying results", qualifying.Count());
            return Ok(qualifying);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached qualifying");
            throw;
        }
    }
}
