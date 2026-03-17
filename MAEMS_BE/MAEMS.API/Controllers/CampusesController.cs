using MAEMS.Application.Features.Campuses.Commands.CreateCampus;
using MAEMS.Application.Features.Campuses.Commands.PatchCampus;
using MAEMS.Application.Features.Campuses.Queries.GetActiveCampuses;
using MAEMS.Application.Features.Campuses.Queries.GetActiveCampusesBasic;
using MAEMS.Application.Features.Campuses.Queries.GetAllCampuses;
using MAEMS.Application.Features.Campuses.Queries.GetCampusById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampusesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampusesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all campuses
    /// </summary>
    /// <returns>List of all campuses</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllCampuses()
    {
        var query = new GetAllCampusesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get campus by ID
    /// </summary>
    /// <param name="id">Campus ID</param>
    /// <returns>Campus details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampusById(int id)
    {
        var query = new GetCampusByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active campuses
    /// </summary>
    /// <returns>List of active campuses</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCampuses()
    {
        var query = new GetActiveCampusesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get basic info of active campuses (CampusId, Name only)
    /// </summary>
    /// <returns>List of active campuses with basic info</returns>
    [HttpGet("active/basic")]
    public async Task<IActionResult> GetActiveCampusesBasic()
    {
        var query = new GetActiveCampusesBasicQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new campus (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateCampus([FromBody] CreateCampusCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch campus (admin only) - partial update
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchCampus(int id, [FromBody] PatchCampusCommand command)
    {
        command.CampusId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
