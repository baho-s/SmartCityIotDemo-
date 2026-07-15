namespace SmartCityIotDemo.Api.Entities;

public class TelemetryData
{
    public int Id { get; set; }

    public string DeviceCode { get; set; } = string.Empty;

    public decimal Temperature { get; set; }

    public decimal Humidity { get; set; }

    public int BatteryLevel { get; set; }

    public int SignalStrength { get; set; }

    public DateTime CreatedAt { get; set; }
}