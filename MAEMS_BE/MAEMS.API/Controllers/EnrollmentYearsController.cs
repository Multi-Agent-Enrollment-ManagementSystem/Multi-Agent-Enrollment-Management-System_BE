using MAEMS.Application.Features.EnrollmentYears.Commands.CreateEnrollmentYear;
using MAEMS.Application.Features.EnrollmentYears.Commands.PatchEnrollmentYear;
using MAEMS.Application.Features.EnrollmentYears.Queries.GetAllEnrollmentYears;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentYearsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentYearsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all enrollment years
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEnrollmentYears()
    {
        var result = await _mediator.Send(new GetAllEnrollmentYearsQuery());

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Create a new enrollment year (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateEnrollmentYear([FromBody] CreateEnrollmentYearCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch enrollment year (admin only) - partial update
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchEnrollmentYear(int id, [FromBody] PatchEnrollmentYearCommand command)
    {
        command.EnrollmentYearId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
