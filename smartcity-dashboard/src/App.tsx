import { useEffect, useState } from "react";

import { getDashboardDevices } from "./services/deviceService";
import { createTelemetryConnection } from "./services/signalRService";

import type {
    DeviceDashboard,
    TelemetryMessage,
} from "./types/device";

function App() {
    const [devices, setDevices] = useState<DeviceDashboard[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const connection = createTelemetryConnection();

        async function initializeDashboard() {
            try {
                // İlk açılışta mevcut verileri API'den al.
                const data = await getDashboardDevices();

                setDevices(data);

                // Backend'den gelecek canlı telemetry verisini dinle.
                connection.on(
                    "TelemetryReceived",
                    (telemetry: TelemetryMessage) => {
                        console.log(
                            "Yeni telemetry geldi:",
                            telemetry
                        );

                        setDevices((currentDevices) =>
                            currentDevices.map((device) => {
                                if (
                                    device.deviceCode !==
                                    telemetry.deviceCode
                                ) {
                                    return device;
                                }

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

            {devices.map((device) => (
                <div key={device.deviceCode}>
                    <h2>{device.name}</h2>

                    <p>
                        Durum:
                        {device.isOnline
                            ? " Online"
                            : " Offline"}
                    </p>

                    <p>Cihaz: {device.deviceCode}</p>

                    <p>Konum: {device.location}</p>

                    <p>
                        Sıcaklık:
                        {" "}
                        {device.temperature ?? "-"} °C
                    </p>

                    <p>
                        Nem:
                        {" "}
                        {device.humidity ?? "-"} %
                    </p>

                    <p>
                        Batarya:
                        {" "}
                        {device.batteryLevel ?? "-"} %
                    </p>

                    <p>
                        Sinyal:
                        {" "}
                        {device.signalStrength ?? "-"} dBm
                    </p>

                    <hr />
                </div>
            ))}
        </main>
    );
}

export default App;