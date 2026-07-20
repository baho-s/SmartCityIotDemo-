import type { DeviceDashboard } from "../types/device";

interface DeviceCardProps {
  device: DeviceDashboard;
}

function DeviceCard({ device }: DeviceCardProps) {
  // Formatlanmış tarih göster
  const formatDate = (date: string | Date) => {
    const d = new Date(date);
    return d.toLocaleString("tr-TR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div className="device-card">
      <div className="device-card-header">
        <h3 className="device-name">{device.deviceName}</h3>
        <div className={`status-indicator ${device.isOnline ? "online" : "offline"}`} />
      </div>

      <p className="device-code">{device.deviceCode}</p>

      <div className="device-info">
        <div className="info-row">
          <span className="info-label">Sıcaklık:</span>
          <span className="info-value">{device.temperature}°C</span>
        </div>
        <div className="info-row">
          <span className="info-label">Nem:</span>
          <span className="info-value">%{device.humidity}</span>
        </div>
        <div className="info-row">
          <span className="info-label">Pil:</span>
          <span className="info-value">%{device.batteryLevel}</span>
        </div>
        <div className="info-row">
          <span className="info-label">Sinyal:</span>
          <span className="info-value">{device.signalStrength}%</span>
        </div>
      </div>

      <div className="device-card-footer">
        <p className="last-seen">
          Son görülme: {formatDate(device.lastSeenAt)}
        </p>
      </div>
    </div>
  );
}

export default DeviceCard;
