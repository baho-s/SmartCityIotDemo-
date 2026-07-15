namespace SmartCityIotDemo.Api.Entities;
public class Device
{
    public int Id { get; set; }

    public string DeviceCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime? LastSeenAt { get; set; }

    public bool IsOnline { get; set; }

    public List<TelemetryData> TelemetryData { get; set; } = new();
}