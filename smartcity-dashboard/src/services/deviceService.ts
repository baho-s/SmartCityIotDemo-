import type {DeviceDashboard} from "../types/device";

// .env dosyasından API'nin URL adresini oku ve değişkene ata
const API_URL = import.meta.env.VITE_API_URL; 

// C#'taki "public async Task<DeviceDashboard[]> GetDashboardDevices()" metodunun aynısı:
export async function getDashboardDevices(): Promise<DeviceDashboard[]> {
    
    // HttpClient ile API'ye git ve cevabı asenkron olarak (await ile) bekle
    const response = await fetch(`${API_URL}/api/devices/dashboard`);

    if (!response.ok) {
        throw new Error("Cihaz verileri alınamadı.");
    }

    return response.json();
}
