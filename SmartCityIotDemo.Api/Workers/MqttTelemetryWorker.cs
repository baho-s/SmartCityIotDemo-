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

/// <summary>
/// .NET'in BackgroundService sınıfından türeyen bu servis, uygulama çalıştığı sürece
/// arka planda asenkron olarak MQTT Broker'ı dinler ve gelen verileri işler.
/// </summary>
public class MqttTelemetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MqttTelemetryWorker> _logger;

    // Sınıfın ihtiyaç duyduğu bağımlılıkları Constructor üzerinden enjekte ediyoruz (Dependency Injection)
    public MqttTelemetryWorker(
        IServiceScopeFactory scopeFactory,     // Singleton içinden Scoped DbContext'e erişebilmek için kullanılan servis fabrikası
        IHubContext<TelemetryHub> hubContext,  // SignalR Hub üzerinden istemcilere anlık mesaj göndermek için hub bağlamı
        IConfiguration configuration,          // appsettings.json dosyasından konfigürasyon verilerini okumak için
        ILogger<MqttTelemetryWorker> logger)   // Çalışma zamanı loglarını console veya log sistemine basmak için
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// BackgroundService çalıştığı sürece arka planda sürekli dönecek olan ana asenkron metot.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // MQTTnet v5 standartlarına uygun olarak MQTT istemci fabrikasını ve kendisini oluşturuyoruz
        var mqttFactory = new MqttClientFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        // appsettings.json içerisindeki MQTT ayarlarını okuyoruz (Yoksa varsayılan değerleri atıyoruz)
        var host = _configuration["Mqtt:Host"] ?? "localhost";
        var port = int.Parse(_configuration["Mqtt:Port"] ?? "1883");
        var topic = _configuration["Mqtt:Topic"] ?? "smartcity/devices/telemetry";

        // MQTT bağlantı seçeneklerini inşa ediyoruz (Server adresi, portu ve istemci kimliği)
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId("smartcity-api-worker")
            .Build();

        // Broker'dan yeni bir mesaj alındığında tetiklenecek olan asenkron olay yöneticisi (Event Handler)
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            // MQTTnet v5 uzantı metodunu kullanarak gelen ham bayt verisini UTF-8 JSON formatında string'e dönüştürüyoruz
            var json = e.ApplicationMessage.ConvertPayloadToString();

            TelemetryMessage? message;

            try
            {
                // Gelen JSON formatındaki string'i C# tarafında işleyebilmek için nesneye deserialize ediyoruz
                message = JsonSerializer.Deserialize<TelemetryMessage>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Büyük/küçük harf duyarlılığını devre dışı bırakır
                    });
            }
            catch (Exception ex)
            {
                // JSON dönüştürme işlemi esnasında bir hata oluşursa logluyor ve o mesajı pas geçiyoruz
                _logger.LogError(ex, "MQTT mesajı deserialize edilemedi. Payload: {Payload}", json);
                return;
            }

            // Gelen mesaj boşsa ya da cihaz kodu bilgisi içermiyorsa geçersiz kabul edip işlem dışı bırakıyoruz
            if (message is null || string.IsNullOrWhiteSpace(message.DeviceCode))
            {
                _logger.LogWarning("Geçersiz telemetry mesajı alındı. Payload: {Payload}", json);
                return;
            }

            // Gelen geçerli verileri veri tabanına kaydetmek ve SignalR ile fırlatmak üzere yardımcı metoda gönderiyoruz
            await SaveTelemetryAsync(message, stoppingToken);
        };

        // Belirlenen konfigürasyon seçenekleriyle MQTT Broker'a asenkron olarak bağlanıyoruz
        await mqttClient.ConnectAsync(options, stoppingToken);

        // Dinlemek istediğimiz konuya (Topic) abone oluyoruz
        await mqttClient.SubscribeAsync(topic, cancellationToken: stoppingToken);

        _logger.LogInformation("MQTT dinleme başladı. Topic: {Topic}", topic);

        // Uygulama ayakta kaldığı sürece bu arka plan servisinin çalışmaya devam etmesini sağlayan ana döngü
        while (!stoppingToken.IsCancellationRequested)
        {
            // CPU'yu %100 kullanıp yormamak için her saniye döngüyü asenkron olarak bekletiyoruz
            await Task.Delay(1000, stoppingToken);
        }
    }

    /// <summary>
    /// Gelen telemetri verilerini veri tabanına işleyen ve istemcilere SignalR ile fırlatan yardımcı metot.
    /// </summary>
    private async Task SaveTelemetryAsync(TelemetryMessage message, CancellationToken cancellationToken)
    {
        // Singleton ömürlü bu sınıfta, Scoped ömre sahip AppDbContext'i güvenle kullanmak için geçici bir scope oluşturuyoruz
        using var scope = _scopeFactory.CreateScope();

        // Geçici kapsam (scope) üzerinden DbContext servis nesnesini talep ediyoruz
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Mesajı gönderen cihaz veri tabanımızda mevcut mu diye kontrol ediyoruz
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(x => x.DeviceCode == message.DeviceCode, cancellationToken);

        if (device is null)
        {
            // Eğer cihaz ilk kez veri gönderiyorsa veri tabanında yeni bir "Cihaz" kaydı (Master Data) oluşturuyoruz
            device = new Device
            {
                DeviceCode = message.DeviceCode,
                Name = $"Device {message.DeviceCode}",
                Location = "Gebze", // Varsayılan konum bilgisi
                IsOnline = true,    // Cihaz veri gönderdiği için online işaretliyoruz
                LastSeenAt = DateTime.UtcNow
            };

            dbContext.Devices.Add(device);
        }
        else
        {
            // Cihaz zaten kayıtlıysa, sadece durumunu aktif edip son görülme zamanını güncelliyoruz
            device.IsOnline = true;
            device.LastSeenAt = DateTime.UtcNow;
        }

        // Cihazdan akan anlık ölçüm bilgilerini (Sıcaklık, Nem, Batarya vb.) tutacak olan telemetry nesnesini hazırlıyoruz
        var telemetry = new TelemetryData
        {
            DeviceCode = message.DeviceCode,
            Temperature = message.Temperature,
            Humidity = message.Humidity,
            BatteryLevel = message.BatteryLevel,
            SignalStrength = message.SignalStrength,
            CreatedAt = DateTime.UtcNow // Ölçümün sisteme girdiği anı kaydediyoruz (Zaman serisi sorguları için çok önemli!)
        };

        // Hazırlanan telemetri verisini tabloya ekleme sırasına alıyoruz
        dbContext.TelemetryData.Add(telemetry);

        // Cihaz durum güncellemesini ve yeni telemetri verisini tek seferde veri tabanına kalıcı olarak yazıyoruz
        await dbContext.SaveChangesAsync(cancellationToken);

        // SignalR kullanarak projenin React tarafına "TelemetryReceived" kanalıyla anlık güncel veriyi fırlatıyoruz
        await _hubContext.Clients.All.SendAsync("TelemetryReceived", new
        {
            message.DeviceCode,
            message.Temperature,
            message.Humidity,
            message.BatteryLevel,
            message.SignalStrength,
            CreatedAt = telemetry.CreatedAt
        }, cancellationToken);

        // Eşik değer analizi: Sıcaklık kritik limiti aştıysa VEYA batarya seviyesi tehlikeli boyuta indiyse alarm ver
        if (message.Temperature > 45 || message.BatteryLevel < 20)
        {
            // SignalR üzerinden "AlarmRaised" kanalını dinleyen tüm istemcilere (React web arayüzlerine) anlık kırmızı alarm düşürüyoruz
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