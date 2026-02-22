using F1Fantasy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace F1Fantasy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PredictionsController : ControllerBase
{
    private readonly PredictionService _predictionService;

    public PredictionsController(PredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new UnauthorizedAccessException("User ID not found");
    }

    // Constructor Championship
    [HttpPost("groups/{groupId}/constructor-championship")]
    public async Task<IActionResult> SaveConstructorChampionship(int groupId, [FromBody] List<string> rankedConstructorIds)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveConstructorChampionshipAsync(groupId, userId, rankedConstructorIds);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/constructor-championship")]
    public async Task<IActionResult> GetConstructorChampionship(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetConstructorChampionshipAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Driver Championship
    [HttpPost("groups/{groupId}/driver-championship")]
    public async Task<IActionResult> SaveDriverChampionship(int groupId, [FromBody] List<string> rankedDriverIds)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveDriverChampionshipAsync(groupId, userId, rankedDriverIds);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/driver-championship")]
    public async Task<IActionResult> GetDriverChampionship(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetDriverChampionshipAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Driver Draft
    [HttpPost("groups/{groupId}/driver-draft")]
    public async Task<IActionResult> SaveDriverDraft(int groupId, [FromBody] DriverDraftRequest request)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveDriverDraftAsync(groupId, userId, request.Driver1Id, request.Driver2Id);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/driver-draft")]
    public async Task<IActionResult> GetDriverDraft(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetDriverDraftAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Destructor
    [HttpPost("groups/{groupId}/destructor")]
    public async Task<IActionResult> SaveDestructor(int groupId, [FromBody] TwoDriverRequest request)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveDestructorAsync(groupId, userId, request.Driver1Id, request.Driver2Id);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/destructor")]
    public async Task<IActionResult> GetDestructor(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetDestructorAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Mr Saturday
    [HttpPost("groups/{groupId}/mr-saturday")]
    public async Task<IActionResult> SaveMrSaturday(int groupId, [FromBody] TwoDriverRequest request)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveMrSaturdayAsync(groupId, userId, request.Driver1Id, request.Driver2Id);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/mr-saturday")]
    public async Task<IActionResult> GetMrSaturday(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetMrSaturdayAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Zero Pointer
    [HttpPost("groups/{groupId}/zero-pointer")]
    public async Task<IActionResult> SaveZeroPointer(int groupId, [FromBody] TwoDriverRequest request)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveZeroPointerAsync(groupId, userId, request.Driver1Id, request.Driver2Id);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/zero-pointer")]
    public async Task<IActionResult> GetZeroPointer(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetZeroPointerAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Wildcard
    [HttpPost("groups/{groupId}/wildcard")]
    public async Task<IActionResult> SaveWildcard(int groupId, [FromBody] WildcardRequest request)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.SaveWildcardAsync(groupId, userId, request.Statement);
            return Ok(prediction);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/wildcard")]
    public async Task<IActionResult> GetWildcard(int groupId)
    {
        try
        {
            var userId = GetUserId();
            var prediction = await _predictionService.GetWildcardAsync(groupId, userId);
            if (prediction == null) return NotFound();
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record DriverDraftRequest(string? Driver1Id, string? Driver2Id);
public record TwoDriverRequest(string? Driver1Id, string? Driver2Id);
public record WildcardRequest(string? Statement);
