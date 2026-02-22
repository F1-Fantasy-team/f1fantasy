using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StandingsController : ControllerBase
{
    private readonly StandingsService _standingsService;
    private readonly ScoringService _scoringService;

    public StandingsController(
        StandingsService standingsService,
        ScoringService scoringService)
    {
        _standingsService = standingsService;
        _scoringService = scoringService;
    }

    [HttpGet("groups/{groupId}")]
    public async Task<IActionResult> GetStandings(int groupId, [FromQuery] string season = "2026")
    {
        try
        {
            var standings = await _standingsService.GetStandingsWithAutoRecalcAsync(groupId, season);
            return Ok(standings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("groups/{groupId}/recalculate")]
    public async Task<IActionResult> RecalculateStandings(int groupId, [FromQuery] string season = "2026")
    {
        try
        {
            await _standingsService.RecalculateStandingsAsync(groupId, season);
            return Ok(new { message = "Standings recalculated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/detailed")]
    public async Task<IActionResult> GetDetailedStandings(int groupId, [FromQuery] string season = "2026")
    {
        try
        {
            var standings = await _standingsService.GetDetailedStandingsAsync(groupId, season);
            return Ok(standings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/user/{userId}/breakdown")]
    public async Task<IActionResult> GetUserBreakdown(int groupId, string userId, [FromQuery] string season = "2026")
    {
        try
        {
            var breakdown = await _scoringService.CalculateDetailedScoresAsync(groupId, userId, season);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/me/breakdown")]
    public async Task<IActionResult> GetMyBreakdown(int groupId, [FromQuery] string season = "2026")
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User ID not found in token" });
            }

            var breakdown = await _scoringService.CalculateDetailedScoresAsync(groupId, userId, season);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
