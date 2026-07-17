public class DeviceDashboardDto
{
    public string DeviceCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public decimal? Temperature { get; set; }

    public decimal? Humidity { get; set; }

    public int? BatteryLevel { get; set; }

    public int? SignalStrength { get; set; }

    public DateTime? TelemetryCreatedAt { get; set; }
}