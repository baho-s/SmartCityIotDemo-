/**
 * Tarihi Türkçe formatında gösterir.
 * @param date - Formatlanacak tarih (string veya Date)
 * @returns Formatlanmış tarih string'i (örn: "20.07.2026 12:32")
 */
export const formatDate = (date: string | Date): string => {
  const d = new Date(date);
  return d.toLocaleString("tr-TR", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};
