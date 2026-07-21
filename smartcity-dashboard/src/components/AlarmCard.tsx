import { memo } from "react";
import type { AlarmMessage } from "../types/device";
import { formatDate } from "../utils/dateFormatter";

interface AlarmCardProps {
  alarm: AlarmMessage;
}

function AlarmCard({ alarm }: AlarmCardProps) {

  return (
    <div className="alarm-card">
      <div className="alarm-card-content">
        <div className="alarm-header">
          <p className="alarm-device">{alarm.deviceCode}</p>
          <p className="alarm-time">{formatDate(alarm.createdAt)}</p>
        </div>
        <p className="alarm-message">{alarm.alarmType}</p>
        <div className="alarm-details">
          <span>Sıcaklık: {alarm.temperature}°C</span>
          <span>Pil: %{alarm.batteryLevel}</span>
        </div>
      </div>
    </div>
  );
}

export default memo(AlarmCard);
