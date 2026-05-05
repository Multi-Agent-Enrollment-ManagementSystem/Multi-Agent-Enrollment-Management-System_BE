using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MAEMS.API.Hubs;
using MAEMS.Application.Features.Reports.Queries.GetSystemPerformance;

namespace MAEMS.API.Services;

/// <summary>
/// Background service to push system performance data to all SignalR clients on a schedule.
/// </summary>
public class SystemPerformancePushService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SystemPerformancePushService> _logger;
    private readonly IHubContext<SystemMonitorHub> _hubContext;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5); // push every 5 seconds

    public SystemPerformancePushService(
        IServiceProvider serviceProvider,
        ILogger<SystemPerformancePushService> logger,
        IHubContext<SystemMonitorHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var query = new GetSystemPerformanceQuery();
                var result = await mediator.Send(query, stoppingToken);
                await _hubContext.Clients.All.SendAsync("ReceiveSystemPerformance", result, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing system performance data via SignalR");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
