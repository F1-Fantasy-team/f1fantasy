using F1Fantasy.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly PredictionRepository _predictionRepository;
    private readonly GroupRepository _groupRepository;

    public AdminController(PredictionRepository predictionRepository, GroupRepository groupRepository)
    {
        _predictionRepository = predictionRepository;
        _groupRepository = groupRepository;
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
                return Forbid("Only group admin can set wildcard points");
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
                return Forbid("Only group admin can mark wildcard as fulfilled");
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
                return Forbid("Only group admin can view all wildcards");
            }

            var wildcards = await _predictionRepository.GetAllWildcardsAsync(groupId);
            return Ok(wildcards);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record SetPointsRequest(int PointsPotential);
public record SetFulfilledRequest(bool Fullfilled);
