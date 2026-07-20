import { useEffect, useState } from "react";
import "./App.css";

import { getDashboardDevices } from "./services/deviceService";
import { createTelemetryConnection } from "./services/signalRService";
import DashboardHeader from "./components/DashboardHeader";
import DeviceCard from "./components/DeviceCard";
import AlarmCard from "./components/AlarmCard";

import type {
    AlarmMessage,
    DeviceDashboard,
    DeviceStatusMessage,
    TelemetryMessage
} from "./types/device";

function App() {
    const [devices, setDevices] = useState<DeviceDashboard[]>([]);
    const [alarms, setAlarms] = useState<AlarmMessage[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);


    useEffect(() => {
        const connection = createTelemetryConnection();

        async function initializeDashboard() {
            try {
                // Sayfa ilk açıldığında mevcut cihaz verilerini API'den al.
                const data = await getDashboardDevices();

                setDevices(data);

                // Backend'den gelen anlık telemetry verilerini dinle.
                connection.on(
                    "TelemetryReceived",
                    (telemetry: TelemetryMessage) => {
                        console.log(
                            "Yeni telemetry geldi:",
                            telemetry
                        );

                        setDevices((currentDevices) =>
                            currentDevices.map((device) => {
                                // Gelen telemetry bu cihaza ait değilse
                                // mevcut cihazı değiştirmeden geri döndür.
                                if (
                                    device.deviceCode !==
                                    telemetry.deviceCode
                                ) {
                                    return device;
                                }

                                // Gelen telemetry ilgili cihaza aitse
                                // mevcut cihaz bilgilerini koruyup
                                // sadece güncel telemetry alanlarını değiştir.
                                return {
                                    ...device,

                                    temperature:
                                        telemetry.temperature,

                                    humidity:
                                        telemetry.humidity,

                                    batteryLevel:
                                        telemetry.batteryLevel,

                                    signalStrength:
                                        telemetry.signalStrength,

                                    telemetryCreatedAt:
                                        telemetry.createdAt,

                                    lastSeenAt:
                                        telemetry.createdAt,

                                    isOnline: true,
                                };
                            })
                        );
                    }
                );

                // Backend'den gelen alarm eventlerini dinle.
                connection.on(
                    "AlarmRaised",
                    (alarm: AlarmMessage) => {
                        console.log(
                            "Yeni alarm geldi:",
                            alarm
                        );

                        // Yeni alarmı listenin başına ekle.
                        setAlarms((currentAlarms) => [
                            alarm,
                            ...currentAlarms,
                        ]);
                    }
                );

                // Backend'den gelen cihaz durum değişikliği eventini dinle.
                connection.on(
                    "DeviceStatusChanged",
                    (status: DeviceStatusMessage) => {
                        console.log("Cihaz durumu değişti:", status);

                        setDevices((currentDevices) =>
                            currentDevices.map((device) => {
                                // Bu event başka bir cihaza aitse
                                // hiçbir değişiklik yapmadan geri döndür.
                                if (device.deviceCode !== status.deviceCode) {
                                    return device;
                                }

                                // Event bu cihaza aitse
                                // yeni bir nesne oluştur ve sadece
                                // değişen alanları güncelle.
                                return {
                                    ...device,
                                    isOnline: status.isOnline,
                                    lastSeenAt: status.lastSeenAt,
                                };
                            })
                        );
                    }
                );

                // SignalR bağlantısını başlat.
                await connection.start();

                console.log(
                    "SignalR bağlantısı kuruldu."
                );
            } catch (error) {
                console.error(error);

                setError(
                    "Dashboard başlatılırken hata oluştu."
                );
            } finally {
                setIsLoading(false);
            }
        }

        initializeDashboard();

        // Component kapatıldığında SignalR bağlantısını temizle.
        return () => {
            connection.stop();
        };
    }, []);

    if (isLoading) {
        return (
            <div className="app-container">
                <div className="loading">Yükleniyor...</div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="app-container">
                <div className="loading" style={{ color: "#EF4444" }}>{error}</div>
            </div>
        );
    }

    return (
        <div className="app-container">
            <DashboardHeader />

            <section className="devices-section">
                <div className="devices-grid">
                    {devices.map((device) => (
                        <DeviceCard key={device.deviceCode} device={device} />
                    ))}
                </div>
            </section>

            <section className="alarms-section">
                <h2 className="alarms-title">Alarmlar</h2>

                {alarms.length === 0 ? (
                    <div className="empty-state">Henüz alarm oluşmadı.</div>
                ) : (
                    <div className="alarms-grid">
                        {alarms.map((alarm, index) => (
                            <AlarmCard
                                key={`${alarm.deviceCode}-${alarm.createdAt}-${index}`}
                                alarm={alarm}
                            />
                        ))}
                    </div>
                )}
            </section>
        </div>
    );
}

export default App;