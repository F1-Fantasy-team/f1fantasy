using Microsoft.AspNetCore.Mvc;
using F1Fantasy.Models;
using F1Fantasy.Services;

namespace F1Fantasy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriverController : ControllerBase
{
    private readonly DriverService _driverService;

    public DriverController(DriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetAllDrivers()
    {
        var drivers = await _driverService.GetAllDriversAsync();
        return Ok(drivers);
    }

    [HttpGet("season/{season}")]
    public async Task<ActionResult<IEnumerable<Driver>>> GetDriversBySeason(string season)
    {
        var drivers = await _driverService.GetDriversBySeasonAsync(season);
        return Ok(drivers);
    }

    [HttpGet("{driverId}")]
    public async Task<ActionResult<Driver>> GetDriverById(string driverId)
    {
        var driver = await _driverService.GetDriverByIdAsync(driverId);
        if (driver == null)
        {
            return NotFound();
        }
        return Ok(driver);
    }

    [HttpGet("cached")]
    public ActionResult<IEnumerable<Driver>> GetCachedDrivers()
    {
        var drivers = _driverService.GetCachedDrivers();
        return Ok(drivers);
    }
}
