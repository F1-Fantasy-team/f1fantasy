using F1Fantasy.Repository;
using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("admin")]
public class AdminController : ControllerBase
{
    private readonly PredictionRepository _predictionRepository;
    private readonly GroupRepository _groupRepository;
    private readonly DriverService _driverService;
    private readonly ConstructorService _constructorService;

    public AdminController(
        PredictionRepository predictionRepository, 
        GroupRepository groupRepository,
        DriverService driverService,
        ConstructorService constructorService)
    {
        _predictionRepository = predictionRepository;
        _groupRepository = groupRepository;
        _driverService = driverService;
        _constructorService = constructorService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new UnauthorizedAccessException("User ID not found");
    }

    private async Task<bool> IsGroupAdminAsync(int groupId, string userId)
    {
        return await _groupRepository.IsUserAdminAsync(groupId, userId);
    }

    [HttpPut("groups/{groupId}/wildcard/{userId}/points")]
    public async Task<IActionResult> SetWildcardPoints(int groupId, string userId, [FromBody] SetPointsRequest request)
    {
        try
        {
            var adminUserId = GetUserId();
            if (!await IsGroupAdminAsync(groupId, adminUserId))
            {
                return StatusCode(403, new { error = "Only group admin can set wildcard points" });
            }

            if (request.PointsPotential < 100 || request.PointsPotential > 200)
            {
                return BadRequest(new { error = "Points must be between 100 and 200" });
            }

            var prediction = await _predictionRepository.GetWildcardAsync(groupId, userId);
            if (prediction == null)
            {
                return NotFound(new { error = "Wildcard prediction not found" });
            }

            prediction.PointsPotential = request.PointsPotential;
            await _predictionRepository.UpsertWildcardAsync(prediction);

            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("groups/{groupId}/wildcard/{userId}/fulfilled")]
    public async Task<IActionResult> SetWildcardFulfilled(int groupId, string userId, [FromBody] SetFulfilledRequest request)
    {
        try
        {
            var adminUserId = GetUserId();
            if (!await IsGroupAdminAsync(groupId, adminUserId))
            {
                return StatusCode(403, new { error = "Only group admin can mark wildcard as fulfilled" });
            }

            var prediction = await _predictionRepository.GetWildcardAsync(groupId, userId);
            if (prediction == null)
            {
                return NotFound(new { error = "Wildcard prediction not found" });
            }

            prediction.Fullfilled = request.Fullfilled;
            await _predictionRepository.UpsertWildcardAsync(prediction);

            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/wildcards")]
    public async Task<IActionResult> GetAllWildcards(int groupId)
    {
        try
        {
            var adminUserId = GetUserId();
            if (!await IsGroupAdminAsync(groupId, adminUserId))
            {
                return StatusCode(403, new { error = "Only group admin can view all wildcards" });
            }

            var wildcards = await _predictionRepository.GetAllWildcardsAsync(groupId);
            return Ok(wildcards);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("populate-season/{season}")]
    public async Task<IActionResult> PopulateSeason(string season)
    {
        try
        {
            // Fetch drivers and constructors for the specified season
            // This will automatically add the season to their ActiveSeasons list
            var drivers = await _driverService.GetDriversBySeasonAsync(season);
            var constructors = await _constructorService.GetConstructorsBySeasonAsync(season);

            return Ok(new 
            { 
                message = $"Successfully populated season {season}",
                driversCount = drivers.Count(),
                constructorsCount = constructors.Count()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record SetPointsRequest(int PointsPotential);
public record SetFulfilledRequest(bool Fullfilled);
