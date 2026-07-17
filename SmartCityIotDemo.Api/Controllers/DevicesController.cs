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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var devices = await _dbContext.Devices
            .AsNoTracking()
            .OrderBy(x => x.DeviceCode)
            .ToListAsync();

        var deviceCodes = devices
            .Select(x => x.DeviceCode)
            .ToList();

        var telemetryData = await _dbContext.TelemetryData
            .AsNoTracking()
            .Where(x => deviceCodes.Contains(x.DeviceCode))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var latestTelemetryByDevice = telemetryData
            .GroupBy(x => x.DeviceCode)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var result = devices.Select(device =>
        {
            latestTelemetryByDevice.TryGetValue(
                device.DeviceCode,
                out var latestTelemetry);

            return new DeviceDashboardDto
            {
                DeviceCode = device.DeviceCode,
                Name = device.Name,
                Location = device.Location,
                IsOnline = device.IsOnline,
                LastSeenAt = device.LastSeenAt,

                Temperature = latestTelemetry?.Temperature,
                Humidity = latestTelemetry?.Humidity,
                BatteryLevel = latestTelemetry?.BatteryLevel,
                SignalStrength = latestTelemetry?.SignalStrength,
                TelemetryCreatedAt = latestTelemetry?.CreatedAt
            };
        });

        return Ok(result);
    }
}