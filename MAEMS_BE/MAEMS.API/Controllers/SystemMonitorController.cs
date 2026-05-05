using MAEMS.Application.Features.Reports.Queries.GetSystemPerformance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MAEMS.API.Hubs;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SystemMonitorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<SystemMonitorHub> _hubContext;

    public SystemMonitorController(IMediator mediator, IHubContext<SystemMonitorHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    /// <summary>
    /// API to monitor system performance, agent activities, and resource usage.
    /// Restricted to users with 'admin' role.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the request</param>
    /// <returns>System performance statistics</returns>
    [HttpGet("performance")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetSystemPerformance(CancellationToken cancellationToken)
    {
        var query = new GetSystemPerformanceQuery();
        var result = await _mediator.Send(query, cancellationToken);
        // Gửi realtime tới tất cả client đang kết nối
        await _hubContext.Clients.All.SendAsync("ReceiveSystemPerformance", result, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}