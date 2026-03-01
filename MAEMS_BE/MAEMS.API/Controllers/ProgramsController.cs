using MAEMS.Application.Features.Programs.Queries.GetActivePrograms;
using MAEMS.Application.Features.Programs.Queries.GetActiveProgramsBasic;
using MAEMS.Application.Features.Programs.Queries.GetAllPrograms;
using MAEMS.Application.Features.Programs.Queries.GetProgramById;
using MediatR;
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
    /// Get all programs with major name
    /// </summary>
    /// <returns>List of all programs</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllPrograms()
    {
        var query = new GetAllProgramsQuery();
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
}
