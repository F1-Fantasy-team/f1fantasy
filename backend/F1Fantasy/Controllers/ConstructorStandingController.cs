using F1Fantasy.Models;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ConstructorStandingController : ControllerBase
{
    private readonly ConstructorStandingService _service;
    private readonly ILogger<ConstructorStandingController> _logger;

    public ConstructorStandingController(ConstructorStandingService service, ILogger<ConstructorStandingController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<ConstructorStandingsList>> GetConstructorStandingsBySeason(string season)
    {
        try
        {
            _logger.LogInformation("Request received: Get constructor standings for season {Season}", season);
            var standings = await _service.GetConstructorStandingsBySeasonAsync(season);
            
            if (standings == null)
            {
                _logger.LogWarning("No constructor standings found for season {Season}", season);
                return NotFound(new ErrorResponse { Message = $"No constructor standings found for season {season}" });
            }

            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for constructor standings season {Season}", season);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("season/{season}/round/{round}")]
    public async Task<ActionResult<ConstructorStandingsList>> GetConstructorStandingsByRound(string season, string round)
    {
        try
        {
            _logger.LogInformation("Request received: Get constructor standings for season {Season} round {Round}", season, round);
            var standings = await _service.GetConstructorStandingsByRoundAsync(season, round);
            
            if (standings == null)
            {
                _logger.LogWarning("No constructor standings found for season {Season} round {Round}", season, round);
                return NotFound(new ErrorResponse { Message = $"No constructor standings found for season {season} round {round}" });
            }

            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for constructor standings season {Season} round {Round}", season, round);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("season/{season}/round/{round}/constructor/{constructorId}")]
    public async Task<ActionResult<ConstructorStandingEntry>> GetConstructorStandingByConstructor(string season, string round, string constructorId)
    {
        try
        {
            _logger.LogInformation("Request received: Get constructor standing for {ConstructorId} season {Season} round {Round}", 
                constructorId, season, round);
            var standing = await _service.GetConstructorStandingByConstructorAsync(season, round, constructorId);
            
            if (standing == null)
            {
                _logger.LogWarning("No constructor standing found for {ConstructorId} season {Season} round {Round}", 
                    constructorId, season, round);
                return NotFound(new ErrorResponse { Message = $"No constructor standing found for {constructorId} in season {season} round {round}" });
            }

            return Ok(standing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for constructor standing {ConstructorId} season {Season} round {Round}", 
                constructorId, season, round);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<List<ConstructorStandingsList>>> GetCachedStandings()
    {
        try
        {
            _logger.LogInformation("Request received: Get all cached constructor standings");
            var standings = await _service.GetCachedStandingsAsync();
            return Ok(standings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for cached constructor standings");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }
}
