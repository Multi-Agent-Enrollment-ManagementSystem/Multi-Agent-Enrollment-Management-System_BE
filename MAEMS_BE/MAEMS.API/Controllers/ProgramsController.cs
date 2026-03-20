using MAEMS.Application.Features.Programs.Commands.CreateProgram;
using MAEMS.Application.Features.Programs.Commands.PatchProgram;
using MAEMS.Application.Features.Programs.Queries.GetActivePrograms;
using MAEMS.Application.Features.Programs.Queries.GetActiveProgramsBasic;
using MAEMS.Application.Features.Programs.Queries.GetAllPrograms;
using MAEMS.Application.Features.Programs.Queries.GetProgramById;
using MAEMS.Application.Features.Programs.Queries.GetProgramsBasicByFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgramsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgramsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all programs with major name (optional filter by majorId and/or enrollmentYearId), with SQL-level sort & paging
    /// </summary>
    /// <param name="majorId">Optional major ID to filter programs</param>
    /// <param name="enrollmentYearId">Optional enrollment year ID to filter programs</param>
    /// <param name="search">Optional search term (matches programName)</param>
    /// <param name="sortBy">Sort field (allowed: programId [default], programName, majorName, enrollmentYear, isActive)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <returns>Paged list of programs</returns>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllPrograms(
        [FromQuery] int? majorId,
        [FromQuery] int? enrollmentYearId,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllProgramsQuery(majorId, enrollmentYearId, search, sortBy, sortDesc, pageNumber, pageSize);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get program by ID with major name
    /// </summary>
    /// <param name="id">Program ID</param>
    /// <returns>Program details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgramById(int id)
    {
        var query = new GetProgramByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active programs with major name
    /// </summary>
    /// <returns>List of active programs</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActivePrograms()
    {
        var query = new GetActiveProgramsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get basic info of active programs (ProgramId, ProgramName, MajorName only)
    /// </summary>
    /// <returns>List of active programs with basic info</returns>
    [HttpGet("active/basic")]
    public async Task<IActionResult> GetActiveProgramsBasic()
    {
        var query = new GetActiveProgramsBasicQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get programs basic info filtered by major id and/or search name, with SQL-level sort & paging
    /// </summary>
    /// <param name="majorId">Optional major ID to filter programs</param>
    /// <param name="searchName">Optional search term to filter by program name</param>
    /// <param name="sortBy">Sort field (allowed: programId [default], programName, majorName)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <returns>Paged list of programs with basic info matching the filters</returns>
    [HttpGet("basic/filter")]
    public async Task<IActionResult> GetProgramsBasicByFilter(
        [FromQuery] int? majorId,
        [FromQuery] string? searchName,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetProgramsBasicByFilterQuery(majorId, searchName, sortBy, sortDesc, pageNumber, pageSize);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new program (admin only)
    /// </summary>
    /// <param name="command">Program information</param>
    /// <returns>Created program</returns>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateProgram([FromBody] CreateProgramCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch program (admin only) - partial update
    /// </summary>
    /// <param name="id">Program ID</param>
    /// <param name="command">Fields to update (only provided fields will be updated)</param>
    /// <returns>Updated program</returns>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchProgram(int id, [FromBody] PatchProgramCommand command)
    {
        command.ProgramId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
