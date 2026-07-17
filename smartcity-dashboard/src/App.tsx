import { useEffect, useState } from "react";

import { getDashboardDevices } from "./services/deviceService";
import { createTelemetryConnection } from "./services/signalRService";

import type {
    AlarmMessage,
    DeviceDashboard,
    TelemetryMessage,
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
        return <p>Yükleniyor...</p>;
    }

    if (error) {
        return <p>{error}</p>;
    }

    return (
        <main>
            <h1>Smart City IoT Dashboard</h1>

            <section>
                <h2>Cihazlar</h2>

                {devices.map((device) => (
                    <div key={device.deviceCode}>
                        <h3>{device.name}</h3>

                        <p>
                            Durum:
                            {device.isOnline
                                ? " Online"
                                : " Offline"}
                        </p>

                        <p>
                            Cihaz: {device.deviceCode}
                        </p>

                        <p>
                            Konum: {device.location}
                        </p>

                        <p>
                            Sıcaklık:{" "}
                            {device.temperature ?? "-"} °C
                        </p>

                        <p>
                            Nem:{" "}
                            {device.humidity ?? "-"} %
                        </p>

                        <p>
                            Batarya:{" "}
                            {device.batteryLevel ?? "-"} %
                        </p>

                        <p>
                            Sinyal:{" "}
                            {device.signalStrength ?? "-"} dBm
                        </p>

                        <hr />
                    </div>
                ))}
            </section>

            <section>
                <h2>Alarmlar</h2>

                {alarms.length === 0 ? (
                    <p>Henüz alarm oluşmadı.</p>
                ) : (
                    alarms.map((alarm, index) => (
                        <div
                            key={`${alarm.deviceCode}-${alarm.createdAt}-${index}`}
                        >
                            <strong>
                                {alarm.deviceCode}
                            </strong>

                            <p>
                                Alarm: {alarm.alarmType}
                            </p>

                            <p>
                                Sıcaklık:{" "}
                                {alarm.temperature} °C
                            </p>

                            <p>
                                Batarya:{" "}
                                %{alarm.batteryLevel}
                            </p>

                            <p>
                                Tarih:{" "}
                                {new Date(
                                    alarm.createdAt
                                ).toLocaleString()}
                            </p>

                            <hr />
                        </div>
                    ))
                )}
            </section>
        </main>
    );
}

export default App;