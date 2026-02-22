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
public class StatusController : ControllerBase
{
    private readonly StatusService _service;
    private readonly ILogger<StatusController> _logger;

    public StatusController(StatusService service, ILogger<StatusController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Status>>> GetAllStatuses()
    {
        try
        {
            _logger.LogInformation("Request received: Get all statuses");
            var statuses = await _service.GetAllStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for all statuses");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<List<Status>>> RefreshStatuses()
    {
        try
        {
            _logger.LogInformation("Request received: Refresh statuses from API");
            var statuses = await _service.RefreshStatusesAsync();
            return Ok(statuses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refresh statuses request");
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("id/{statusId}")]
    public async Task<ActionResult<Status>> GetStatusById(string statusId)
    {
        try
        {
            _logger.LogInformation("Request received: Get status by ID {StatusId}", statusId);
            var status = await _service.GetByIdAsync(statusId);
            
            if (status == null)
            {
                _logger.LogWarning("Status not found: {StatusId}", statusId);
                return NotFound(new ErrorResponse { Message = $"Status with ID {statusId} not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for status ID {StatusId}", statusId);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }

    [HttpGet("text/{statusText}")]
    public async Task<ActionResult<Status>> GetStatusByText(string statusText)
    {
        try
        {
            _logger.LogInformation("Request received: Get status by text '{StatusText}'", statusText);
            var status = await _service.GetByTextAsync(statusText);
            
            if (status == null)
            {
                _logger.LogWarning("Status not found: '{StatusText}'", statusText);
                return NotFound(new ErrorResponse { Message = $"Status '{statusText}' not found" });
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for status text '{StatusText}'", statusText);
            return StatusCode(500, new ErrorResponse { Message = "An error occurred while processing your request" });
        }
    }
}
