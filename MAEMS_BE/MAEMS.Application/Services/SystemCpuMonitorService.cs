using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MAEMS.Application.Services;

public class SystemCpuMonitorService : BackgroundService
{
    public static double CpuUsagePercentage { get; private set; } = 0;
    private static readonly object _lock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var process = Process.GetCurrentProcess();
        var lastCpuTime = process.TotalProcessorTime;
        var lastTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken); // đo mỗi 2s

            var nowCpuTime = process.TotalProcessorTime;
            var now = DateTime.UtcNow;

            var cpuUsedMs = (nowCpuTime - lastCpuTime).TotalMilliseconds;
            var totalMsPassed = (now - lastTime).TotalMilliseconds;
            var cpuUsage = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            var percent = Math.Round(cpuUsage * 100, 2);

            lock (_lock)
            {
                CpuUsagePercentage = percent;
            }

            lastCpuTime = nowCpuTime;
            lastTime = now;
        }
    }

    public static double GetCpuUsage()
    {
        lock (_lock)
        {
            return CpuUsagePercentage;
        }
    }
}
