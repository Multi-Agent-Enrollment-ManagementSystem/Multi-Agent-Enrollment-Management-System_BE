using MAEMS.Application.Features.AdmissionTypes.Commands.CreateAdmissionType;
using MAEMS.Application.Features.AdmissionTypes.Commands.PatchAdmissionType;
using MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypesBasicByFilter;
using MAEMS.Application.Features.AdmissionTypes.Queries.GetAllAdmissionTypes;
using MAEMS.Application.Features.AdmissionTypes.Queries.GetActiveAdmissionTypes;
using MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypeById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdmissionTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdmissionTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all admission types
    /// </summary>
    /// <returns>List of all admission types with full details</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllAdmissionTypes()
    {
        var query = new GetAllAdmissionTypesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all active admission types
    /// </summary>
    /// <returns>List of active admission types with full details</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAdmissionTypes()
    {
        var query = new GetActiveAdmissionTypesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get admission type by ID
    /// </summary>
    /// <param name="id">Admission type ID</param>
    /// <returns>Admission type details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAdmissionTypeById(int id)
    {
        var query = new GetAdmissionTypeByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get admission types basic info (AdmissionTypeId, AdmissionTypeName, Type) filtered by EnrollmentYearId
    /// </summary>
    /// <param name="enrollmentYearId">Optional enrollment year ID to filter admission types</param>
    /// <returns>List of admission types with basic info matching the filter</returns>
    [HttpGet("active/basic")]
    public async Task<IActionResult> GetAdmissionTypesBasicByFilter([FromQuery] int? enrollmentYearId)
    {
        var query = new GetAdmissionTypesBasicByFilterQuery(enrollmentYearId);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create admission type (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateAdmissionType([FromBody] CreateAdmissionTypeCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch admission type (admin only) - partial update
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchAdmissionType(int id, [FromBody] PatchAdmissionTypeCommand command)
    {
        command.AdmissionTypeId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
