using MAEMS.Application.Features.Majors.Queries.GetActiveMajors;
using MAEMS.Application.Features.Majors.Queries.GetActiveMajorsBasic;
using MAEMS.Application.Features.Majors.Queries.GetAllMajors;
using MAEMS.Application.Features.Majors.Queries.GetMajorById;
using MediatR;
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
    /// Get all majors
    /// </summary>
    /// <returns>List of all majors</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllMajors()
    {
        var query = new GetAllMajorsQuery();
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
}
