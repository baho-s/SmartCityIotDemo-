using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCityIotDemo.Api.Data;

namespace SmartCityIotDemo.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DevicesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetDevices()
    {
        var devices = await _dbContext.Devices
            .OrderBy(x => x.DeviceCode)
            .Select(x => new
            {
                x.DeviceCode,
                x.Name,
                x.Location,
                x.IsOnline,
                x.LastSeenAt
            })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpGet("{deviceCode}/telemetry/latest")]
    public async Task<IActionResult> GetLatestTelemetry(string deviceCode)
    {
        var latest = await _dbContext.TelemetryData
            .Where(x => x.DeviceCode == deviceCode)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (latest is null)
            return NotFound();

        return Ok(latest);
    }

    [HttpGet("{deviceCode}/telemetry")]
    public async Task<IActionResult> GetTelemetryHistory(string deviceCode)
    {
        var data = await _dbContext.TelemetryData
            .Where(x => x.DeviceCode == deviceCode)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(data);
    }
}