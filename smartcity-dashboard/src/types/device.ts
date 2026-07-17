export interface DeviceDashboard{
    deviceCode: string;
    name: string;
    location:string;
    isOnline: boolean;
    lastSeenAt: string | null;

    temperature: number | null;
    humidity: number | null;
    batteryLevel: number | null;
    signalStrength: number | null;

    telemetryCreatedAt: string | null;
}

export interface TelemetryMessage {
    deviceCode: string;
    temperature: number;
    humidity: number;
    batteryLevel: number;
    signalStrength: number;
    createdAt: string;
}

export interface AlarmMessage {
    deviceCode: string;
    alarmType: string;
    temperature: number;
    batteryLevel: number;
    createdAt: string;
}

