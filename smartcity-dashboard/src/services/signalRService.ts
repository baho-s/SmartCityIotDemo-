import {
    HubConnectionBuilder,
    LogLevel,
} from "@microsoft/signalr";

const API_URL=import.meta.env.VITE_API_URL;

export function createTelemetryConnection() {
    return new HubConnectionBuilder()
        .withUrl(`${API_URL}/hubs/telemetry`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build();
}