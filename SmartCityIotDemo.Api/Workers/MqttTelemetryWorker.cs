using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using SmartCityIotDemo.Api.Data;
using SmartCityIotDemo.Api.Entities;
using SmartCityIotDemo.Api.Hubs;
using SmartCityIotDemo.Api.Models;

namespace SmartCityIotDemo.Api.Workers;

public class MqttTelemetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttTelemetryWorker> _logger;

    public MqttTelemetryWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<TelemetryHub> hubContext,
        IConfiguration configuration,
        ILogger<MqttTelemetryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttClientFactory();

        var mqttClient = mqttFactory.CreateMqttClient();

        var host = _configuration["Mqtt:Host"] ?? "localhost";
        var port = int.Parse(_configuration["Mqtt:Port"] ?? "1883");
        var topic = _configuration["Mqtt:Topic"] ?? "smartcity/devices/telemetry";

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("smartcity-api-worker")
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var json = e.ApplicationMessage.ConvertPayloadToString();

            TelemetryMessage? message;

            try
            {
                message = JsonSerializer.Deserialize<TelemetryMessage>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT mesajı deserialize edilemedi. Payload: {Payload}", json);
                return;
            }

            if (message is null || string.IsNullOrWhiteSpace(message.DeviceCode))
            {
                _logger.LogWarning("Geçersiz telemetry mesajı alındı. Payload: {Payload}", json);
                return;
            }

            await SaveTelemetryAsync(message, stoppingToken);
        };

        await mqttClient.ConnectAsync(options, stoppingToken);

        await mqttClient.SubscribeAsync(topic, cancellationToken: stoppingToken);

        _logger.LogInformation("MQTT dinleme başladı. Topic: {Topic}", topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task SaveTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(x => x.DeviceCode == message.DeviceCode, cancellationToken);

        if (device is null)
        {
            device = new Device
            {
                DeviceCode = message.DeviceCode,
                Name = $"Device {message.DeviceCode}",
                Location = "Gebze",
                IsOnline = true,
                LastSeenAt = DateTime.UtcNow
            };

            dbContext.Devices.Add(device);
        }
        else
        {
            device.IsOnline = true;
            device.LastSeenAt = DateTime.UtcNow;
        }

        var telemetry = new TelemetryData
        {
            DeviceCode = message.DeviceCode,
            Temperature = message.Temperature,
            Humidity = message.Humidity,
            BatteryLevel = message.BatteryLevel,
            SignalStrength = message.SignalStrength,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.TelemetryData.Add(telemetry);

        await dbContext.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.All.SendAsync("TelemetryReceived", new
        {
            message.DeviceCode,
            message.Temperature,
            message.Humidity,
            message.BatteryLevel,
            message.SignalStrength,
            CreatedAt = telemetry.CreatedAt
        }, cancellationToken);

        if (message.Temperature > 45 || message.BatteryLevel < 20)
        {
            await _hubContext.Clients.All.SendAsync("AlarmRaised", new
            {
                message.DeviceCode,
                AlarmType = message.Temperature > 45 ? "HIGH_TEMPERATURE" : "LOW_BATTERY",
                message.Temperature,
                message.BatteryLevel,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
    }
}