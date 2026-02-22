using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("read")]
public class ResultController : ControllerBase
{
    private readonly ResultService _resultService;
    private readonly ILogger<ResultController> _logger;

    public ResultController(ResultService resultService, ILogger<ResultController> logger)
    {
        _resultService = resultService;
        _logger = logger;
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<RaceWithResults>>> GetResultsBySeason(string season)
    {
        try
        {
            _logger.LogInformation("GET /api/result/season/{Season} - Fetching results for season", season);
            var results = await _resultService.GetResultsBySeasonAsync(season);
            _logger.LogInformation("Successfully retrieved results for {Count} races in season {Season}", 
                results.Count(), season);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching results for season {Season}", season);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<RaceWithResults>> GetResultsByRace(string season, string round)
    {
        try
        {
            _logger.LogInformation("GET /api/result/season/{Season}/round/{Round} - Fetching results for race", 
                season, round);
            var result = await _resultService.GetResultsByRaceAsync(season, round);
            
            if (result == null)
            {
                _logger.LogWarning("No results found for season {Season}, round {Round}", season, round);
                return NotFound(new { message = $"No results found for season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} results for season {Season}, round {Round}", 
                result.Results?.Count ?? 0, season, round);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching results for season {Season}, round {Round}", season, round);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}/driver/{driverId}")]
    public async Task<ActionResult<Result>> GetResultByDriver(string season, string round, string driverId)
    {
        try
        {
            _logger.LogInformation("GET /api/result/season/{Season}/round/{Round}/driver/{DriverId} - Fetching result", 
                season, round, driverId);
            var result = await _resultService.GetResultByDriverAsync(season, round, driverId);
            
            if (result == null)
            {
                _logger.LogWarning("No result found for season {Season}, round {Round}, driver {DriverId}", 
                    season, round, driverId);
                return NotFound(new { message = $"No result found for driver {driverId} in season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved result for driver {DriverId} in season {Season}, round {Round}", 
                driverId, season, round);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching result for season {Season}, round {Round}, driver {DriverId}", 
                season, round, driverId);
            throw;
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<Result>>> GetCachedResults()
    {
        try
        {
            _logger.LogInformation("GET /api/result/cached - Fetching cached results");
            var results = await _resultService.GetCachedResultsAsync();
            
            if (!results.Any())
            {
                _logger.LogWarning("No cached results found. Database may be empty.");
                return Ok(new { 
                    message = "No cached results found. Try calling /api/result/season/2025 first to populate the cache.",
                    results = results 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached results", results.Count());
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached results");
            throw;
        }
    }

    [HttpGet("season/{season}/sprint")]
    public async Task<ActionResult<IEnumerable<RaceWithResults>>> GetSprintResultsBySeason(string season)
    {
        try
        {
            _logger.LogInformation("GET /api/result/season/{Season}/sprint - Fetching sprint results for season", season);
            var results = await _resultService.GetSprintResultsBySeasonAsync(season);
            _logger.LogInformation("Successfully retrieved sprint results for {Count} races in season {Season}", 
                results.Count(), season);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sprint results for season {Season}", season);
            throw;
        }
    }

    [HttpGet("season/{season}/round/{round}/sprint")]
    public async Task<ActionResult<RaceWithResults>> GetSprintResultsByRace(string season, string round)
    {
        try
        {
            _logger.LogInformation("GET /api/result/season/{Season}/round/{Round}/sprint - Fetching sprint results for race", 
                season, round);
            var result = await _resultService.GetSprintResultsByRaceAsync(season, round);
            
            if (result == null)
            {
                _logger.LogWarning("No sprint results found for season {Season}, round {Round}", season, round);
                return NotFound(new { message = $"No sprint results found for season {season}, round {round}" });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} sprint results for season {Season}, round {Round}", 
                result.SprintResults?.Count ?? 0, season, round);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sprint results for season {Season}, round {Round}", season, round);
            throw;
        }
    }
}
