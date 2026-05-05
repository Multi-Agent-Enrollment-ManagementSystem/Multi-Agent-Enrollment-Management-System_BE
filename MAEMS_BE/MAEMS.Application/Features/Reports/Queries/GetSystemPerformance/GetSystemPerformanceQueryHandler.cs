using System.Diagnostics;
using MAEMS.Application.DTOs.SystemMonitor;
using MAEMS.Application.Services;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using System.Threading;

namespace MAEMS.Application.Features.Reports.Queries.GetSystemPerformance;

public sealed class GetSystemPerformanceQueryHandler : IRequestHandler<GetSystemPerformanceQuery, BaseResponse<SystemPerformanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSystemPerformanceQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<SystemPerformanceDto>> Handle(GetSystemPerformanceQuery request, CancellationToken cancellationToken)
    {
        Process currentProcess = Process.GetCurrentProcess();
        TimeSpan uptime = DateTime.UtcNow - currentProcess.StartTime.ToUniversalTime();

        // Đọc giá trị CPU đã cache từ background service
        double cpuUsagePercentage = SystemCpuMonitorService.GetCpuUsage();
        double memoryUsageMb = currentProcess.WorkingSet64 / (1024.0 * 1024.0);

        // Đếm AgentLog trong ngày (filter tại DB, không load hết bảng)
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        int totalAgentLogsToday = await CountAgentLogsByDate(todayUtc, tomorrowUtc, cancellationToken);

        // GC và ThreadPool
        int gcGen2Collections = GC.CollectionCount(2);
        ThreadPool.GetAvailableThreads(out int workerThreads, out _);
        ThreadPool.GetMaxThreads(out int maxThreads, out _);
        int threadPoolBusyWorkers = maxThreads - workerThreads;

        var dto = new SystemPerformanceDto
        {
            CpuUsagePercentage = cpuUsagePercentage,
            MemoryUsageMb = memoryUsageMb,
            GcGen2Collections = gcGen2Collections,
            ThreadPoolBusyWorkers = threadPoolBusyWorkers,
            Uptime = uptime,
            TotalAgentLogsToday = totalAgentLogsToday,
        };

        return BaseResponse<SystemPerformanceDto>.SuccessResponse(dto, "System performance retrieved");
    }

    // Helper: Đếm AgentLog theo ngày tại DB
    private async Task<int> CountAgentLogsByDate(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        // Sử dụng truy vấn SQL-level lọc theo CreatedAt
        return await _unitOfWork.AgentLogs.CountByCreatedAtAsync(fromUtc, toUtc, cancellationToken);
    }
}
