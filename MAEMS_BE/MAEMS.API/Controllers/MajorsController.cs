using MAEMS.Application.Features.Majors.Commands.CreateMajor;
using MAEMS.Application.Features.Majors.Commands.PatchMajor;
using MAEMS.Application.Features.Majors.Queries.GetActiveMajors;
using MAEMS.Application.Features.Majors.Queries.GetActiveMajorsBasic;
using MAEMS.Application.Features.Majors.Queries.GetAllMajors;
using MAEMS.Application.Features.Majors.Queries.GetMajorById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MajorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MajorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all majors, with SQL-level sort & paging
    /// </summary>
    /// <param name="search">Optional search term (matches majorCode/majorName)</param>
    /// <param name="sortBy">Sort field (allowed: majorId [default], majorCode, majorName, createdAt, isActive)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <returns>Paged list of all majors</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllMajors(
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllMajorsQuery(search, sortBy, sortDesc, pageNumber, pageSize);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get major by ID
    /// </summary>
    /// <param name="id">Major ID</param>
    /// <returns>Major details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMajorById(int id)
    {
        var query = new GetMajorByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active majors
    /// </summary>
    /// <returns>List of active majors</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveMajors()
    {
        var query = new GetActiveMajorsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get basic info of active majors (MajorId, MajorCode, MajorName only)
    /// </summary>
    /// <returns>List of active majors with basic info</returns>
    [HttpGet("active/basic")]
    public async Task<IActionResult> GetActiveMajorsBasic()
    {
        var query = new GetActiveMajorsBasicQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new major (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateMajor([FromBody] CreateMajorCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch major (admin only) - partial update
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchMajor(int id, [FromBody] PatchMajorCommand command)
    {
        command.MajorId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
