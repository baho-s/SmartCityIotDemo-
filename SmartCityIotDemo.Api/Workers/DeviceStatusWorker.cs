
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartCityIotDemo.Api.Data;
using SmartCityIotDemo.Api.Hubs;

namespace SmartCityIotDemo.Api.Workers
{
    public class DeviceStatusWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DeviceStatusWorker> _logger;
        private readonly IHubContext<TelemetryHub> _hubContext;

        public DeviceStatusWorker(IServiceScopeFactory scopeFactory, ILogger<DeviceStatusWorker> logger, IHubContext<TelemetryHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<AppDbContext>();

                    var threshold = DateTime.UtcNow.AddSeconds(-15);

                    var offlineDevices = await dbContext.Devices
                        .Where(device =>
                            device.IsOnline &&
                            device.LastSeenAt < threshold)
                        .ToListAsync(stoppingToken);

                    if (offlineDevices.Any())
                    {
                        foreach (var device in offlineDevices)
                        {
                            device.IsOnline = false;
                        }

                        await dbContext.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "{Count} cihaz offline olarak işaretlendi.",
                            offlineDevices.Count);

                        foreach (var device in offlineDevices)
                        {
                            await _hubContext.Clients.All.SendAsync(
                                "DeviceStatusChanged",
                                new
                                {
                                    device.DeviceCode,
                                    device.IsOnline,
                                    device.LastSeenAt
                                },
                                stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "DeviceStatusWorker hata verdi.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }


    }
}
