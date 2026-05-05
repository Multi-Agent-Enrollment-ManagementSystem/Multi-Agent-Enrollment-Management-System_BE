namespace MAEMS.Application.DTOs.SystemMonitor;

public class SystemPerformanceDto
{
    // ── Tài nguyên ──────────────────────────────
    public double CpuUsagePercentage   { get; set; }  // % CPU process đang dùng
    public double MemoryUsageMb        { get; set; }  // RAM vật lý (Working Set)

    // ── Sức khoẻ runtime ────────────────────────
    public int    GcGen2Collections    { get; set; }  // Gen2 cao = áp lực memory nghiêm trọng
    public int    ThreadPoolBusyWorkers{ get; set; }  // Gần max = nguy cơ thread starvation

    // ── Thông tin chung ─────────────────────────
    public TimeSpan Uptime             { get; set; }  // Thời gian server đã chạy
    public int    TotalAgentLogsToday  { get; set; }  // Hoạt động nghiệp vụ trong ngày
}
