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
    /// Get all programs with major name (optional filter by majorId and/or enrollmentYearId)
    /// </summary>
    /// <param name="majorId">Optional major ID to filter programs</param>
    /// <param name="enrollmentYearId">Optional enrollment year ID to filter programs</param>
    /// <returns>List of programs</returns>
    [HttpGet]
    [Authorize(Roles = "admin")]

    public async Task<IActionResult> GetAllPrograms([FromQuery] int? majorId, [FromQuery] int? enrollmentYearId)
    {
        var query = new GetAllProgramsQuery(majorId, enrollmentYearId);
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
    /// Get programs basic info filtered by major id and/or search name
    /// </summary>
    /// <param name="majorId">Optional major ID to filter programs</param>
    /// <param name="searchName">Optional search term to filter by program name</param>
    /// <returns>List of programs with basic info matching the filters</returns>
    [HttpGet("basic/filter")]
    public async Task<IActionResult> GetProgramsBasicByFilter([FromQuery] int? majorId, [FromQuery] string? searchName)
    {
        var query = new GetProgramsBasicByFilterQuery(majorId, searchName);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
