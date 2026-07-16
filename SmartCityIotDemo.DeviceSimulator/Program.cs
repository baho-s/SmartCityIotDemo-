using System.Text;
using System.Text.Json;
using MQTTnet;

var mqttFactory = new MqttClientFactory();
var mqttClient = mqttFactory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithClientId("device-simulator")
    .Build();

await mqttClient.ConnectAsync(options);

var random = new Random();

var devices = new[]
{
    "LAMP-001",
    "LAMP-002",
    "PARK-001",
    "WATER-001"
};

Console.WriteLine("Device simulator başladı.");

while (true)
{
    var deviceCode = devices[random.Next(devices.Length)];

    var message = new
    {
        DeviceCode = deviceCode,
        Temperature = Math.Round(20 + random.NextDouble() * 35, 2),
        Humidity = Math.Round(40 + random.NextDouble() * 40, 2),
        BatteryLevel = random.Next(5, 100),
        SignalStrength = random.Next(-100, -40),
        SentAt = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(message);

    var mqttMessage = new MqttApplicationMessageBuilder()
        .WithTopic("smartcity/devices/telemetry")
        .WithPayload(Encoding.UTF8.GetBytes(json))
        .Build();

    await mqttClient.PublishAsync(mqttMessage);

    Console.WriteLine(json);

    await Task.Delay(2000);
}