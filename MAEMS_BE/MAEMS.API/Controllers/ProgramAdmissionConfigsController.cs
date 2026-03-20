using MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.CreateProgramAdmissionConfig;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.PatchProgramAdmissionConfig;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetActiveProgramAdmissionConfigs;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigById;
using MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigsByFilter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
    /// Get all program admission configs with ProgramName, CampusName and AdmissionTypeName, with SQL-level sort & paging
    /// </summary>
    /// <param name="programId">Optional program ID to filter</param>
    /// <param name="campusId">Optional campus ID to filter</param>
    /// <param name="admissionTypeId">Optional admission type ID to filter</param>
    /// <param name="search">Optional search term (matches program/campus/admissionType name)</param>
    /// <param name="sortBy">Sort field (allowed: configId [default], programName, campusName, admissionTypeName, quota, isActive, createdAt)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    /// <returns>Paged list of program admission configs</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllProgramAdmissionConfigs(
        [FromQuery] int? programId,
        [FromQuery] int? campusId,
        [FromQuery] int? admissionTypeId,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllProgramAdmissionConfigsQuery(
            programId,
            campusId,
            admissionTypeId,
            search,
            sortBy,
            sortDesc,
            pageNumber,
            pageSize);

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

    /// <summary>
    /// Create program admission config (admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateProgramAdmissionConfig([FromBody] CreateProgramAdmissionConfigCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Patch program admission config (admin only) - partial update
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchProgramAdmissionConfig(int id, [FromBody] PatchProgramAdmissionConfigCommand command)
    {
        command.ConfigId = id;

        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
