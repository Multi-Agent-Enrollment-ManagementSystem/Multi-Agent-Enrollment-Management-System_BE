using MAEMS.Application.Features.Roles.Queries.GetActiveRoles;
using MAEMS.Application.Features.Roles.Queries.GetAllRoles;
using MAEMS.Application.Features.Roles.Queries.GetRoleById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <returns>List of all roles</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var query = new GetAllRolesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">Role ID</param>
    /// <returns>Role details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(int id)
    {
        var query = new GetRoleByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active roles
    /// </summary>
    /// <returns>List of active roles</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveRoles()
    {
        var query = new GetActiveRolesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
