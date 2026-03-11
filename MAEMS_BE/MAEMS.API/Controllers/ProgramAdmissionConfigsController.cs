using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetActiveProgramAdmissionConfigs;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigById;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigsByFilter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgramAdmissionConfigsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgramAdmissionConfigsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all program admission configs with ProgramName, CampusName and AdmissionTypeName
    /// </summary>
    /// <returns>List of all program admission configs</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllProgramAdmissionConfigs()
    {
        var query = new GetAllProgramAdmissionConfigsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all active program admission configs
    /// </summary>
    /// <returns>List of active program admission configs</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveProgramAdmissionConfigs()
    {
        var query = new GetActiveProgramAdmissionConfigsQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get program admission config by ID
    /// </summary>
    /// <param name="id">Config ID</param>
    /// <returns>Program admission config details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgramAdmissionConfigById(int id)
    {
        var query = new GetProgramAdmissionConfigByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get program admission configs filtered by programId, campusId and/or admissionTypeId
    /// </summary>
    /// <param name="programId">Optional program ID to filter</param>
    /// <param name="campusId">Optional campus ID to filter</param>
    /// <param name="admissionTypeId">Optional admission type ID to filter</param>
    /// <returns>List of program admission configs matching the filters</returns>
    [HttpGet("filter")]
    public async Task<IActionResult> GetProgramAdmissionConfigsByFilter(
        [FromQuery] int? programId,
        [FromQuery] int? campusId,
        [FromQuery] int? admissionTypeId)
    {
        var query = new GetProgramAdmissionConfigsByFilterQuery(programId, campusId, admissionTypeId);
        var result = await _mediator.Send(query);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
