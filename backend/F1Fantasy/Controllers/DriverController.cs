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
public class DriverController : ControllerBase
{
    private readonly DriverService _driverService;
    private readonly ILogger<DriverController> _logger;

    public DriverController(DriverService driverService, ILogger<DriverController> logger)
    {
        _driverService = driverService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetAllDrivers()
    {
        try
        {
            _logger.LogInformation("GET /api/driver - Fetching all drivers");
            var drivers = await _driverService.GetAllDriversAsync();
            _logger.LogInformation("Successfully retrieved {Count} total drivers", drivers.Count());
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all drivers");
            throw;
        }
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<Driver>>> GetDriversBySeason(string season)
    {
        try
        {
            _logger.LogInformation("GET /api/driver/season/{Season} - Fetching drivers for season", season);
            var drivers = await _driverService.GetDriversBySeasonAsync(season);
            _logger.LogInformation("Successfully retrieved {Count} drivers for season {Season}", drivers.Count(), season);
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching drivers for season {Season}", season);
            throw;
        }
    }

    [HttpGet("{driverId}")]
    public async Task<ActionResult<Driver>> GetDriverById(string driverId)
    {
        try
        {
            _logger.LogInformation("GET /api/driver/{DriverId} - Fetching driver by ID", driverId);
            var driver = await _driverService.GetDriverByIdAsync(driverId);
            if (driver == null)
            {
                _logger.LogWarning("Driver not found: {DriverId}", driverId);
                return NotFound(new { message = $"Driver not found: {driverId}" });
            }
            _logger.LogInformation("Successfully retrieved driver {DriverId}", driverId);
            return Ok(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver {DriverId}", driverId);
            throw;
        }
    }

    [HttpGet("cached")]
    public async Task<ActionResult<IEnumerable<Driver>>> GetCachedDrivers()
    {
        try
        {
            _logger.LogInformation("GET /api/driver/cached - Fetching cached drivers");
            var drivers = await _driverService.GetCachedDriversAsync();
            
            if (!drivers.Any())
            {
                _logger.LogWarning("No cached drivers found. Database may be empty.");
                return Ok(new { 
                    message = "No cached drivers found. Try calling /api/driver/season/2024 first to populate the cache.",
                    drivers = drivers 
                });
            }
            
            _logger.LogInformation("Successfully retrieved {Count} cached drivers", drivers.Count());
            return Ok(drivers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cached drivers");
            throw;
        }
    }
}
