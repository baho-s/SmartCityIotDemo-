import type { AlarmMessage } from "../types/device";

interface AlarmCardProps {
  alarm: AlarmMessage;
}

function AlarmCard({ alarm }: AlarmCardProps) {
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
    <div className="alarm-card">
      <div className="alarm-card-content">
        <div className="alarm-header">
          <p className="alarm-device">{alarm.deviceName}</p>
          <p className="alarm-time">{formatDate(alarm.createdAt)}</p>
        </div>
        <p className="alarm-message">{alarm.message}</p>
        <p className="alarm-code">{alarm.deviceCode}</p>
      </div>
    </div>
  );
}

export default AlarmCard;
