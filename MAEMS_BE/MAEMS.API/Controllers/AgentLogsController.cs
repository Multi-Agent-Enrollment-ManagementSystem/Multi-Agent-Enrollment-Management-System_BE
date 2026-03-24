using MAEMS.Application.Features.AgentLogs.Queries.GetAllAgentLogs;
using MAEMS.Application.Features.AgentLogs.Queries.GetAgentLogById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all agent logs with pagination, sorting, and filtering (admin and QA only)
    /// </summary>
    /// <param name="applicationId">Optional filter by application ID</param>
    /// <param name="documentId">Optional filter by document ID</param>
    /// <param name="agentType">Optional filter by agent type (supports partial match)</param>
    /// <param name="status">Optional filter by status (supports partial match)</param>
    /// <param name="search">Optional search in agentType, action, status, outputData fields</param>
    /// <param name="sortBy">Sort field: logId, applicationId, documentId, agentType, action, status, createdAt (default: logId)</param>
    /// <param name="sortDesc">Sort in descending order (default: false)</param>
    /// <param name="pageNumber">Page number (default: 1, min: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paged list of agent logs</returns>
    [HttpGet]
    [Authorize(Roles = "admin,QA")]
    public async Task<IActionResult> GetAllAgentLogs(
        [FromQuery] int? applicationId = null,
        [FromQuery] int? documentId = null,
        [FromQuery] string? agentType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllAgentLogsQuery(
            applicationId,
            documentId,
            agentType,
            status,
            search,
            sortBy,
            sortDesc,
            pageNumber,
            pageSize);

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get agent log by ID (admin and QA only)
    /// </summary>
    /// <param name="id">Agent log ID</param>
    /// <returns>Agent log details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,QA")]
    public async Task<IActionResult> GetAgentLogById(int id)
    {
        var query = new GetAgentLogByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
