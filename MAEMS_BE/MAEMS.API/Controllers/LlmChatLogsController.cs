using MAEMS.Application.Features.LlmChatLogs.Queries.GetAllLlmChatLogs;
using MAEMS.Application.Features.LlmChatLogs.Queries.GetLlmChatLogById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmChatLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LlmChatLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all LLM chat logs with pagination, sorting, and filtering (admin and QA only)
    /// </summary>
    /// <param name="userId">Optional filter by user ID</param>
    /// <param name="userQuery">Optional filter by user query (supports partial match)</param>
    /// <param name="search">Optional search in userQuery, message, llmResponse fields</param>
    /// <param name="sortBy">Sort field: chatId, userId, userQuery, message, llmResponse, createdAt (default: chatId)</param>
    /// <param name="sortDesc">Sort in descending order (default: false)</param>
    /// <param name="pageNumber">Page number (default: 1, min: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Paged list of LLM chat logs</returns>
    [HttpGet]
    [Authorize(Roles = "admin,qa")]
    public async Task<IActionResult> GetAllLlmChatLogs(
        [FromQuery] int? userId = null,
        [FromQuery] string? userQuery = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllLlmChatLogsQuery(
            userId,
            userQuery,
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
    /// Get LLM chat log by ID (admin and QA only)
    /// </summary>
    /// <param name="id">LLM chat log ID</param>
    /// <returns>LLM chat log details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "admin,qa")]
    public async Task<IActionResult> GetLlmChatLogById(int id)
    {
        var query = new GetLlmChatLogByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

