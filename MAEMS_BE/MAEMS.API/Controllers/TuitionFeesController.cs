using MAEMS.Application.Features.TuitionFees.Queries.GetTuitionFeeCalculation;
using MAEMS.Application.Features.TuitionFees.Queries.GetAllActiveTuitionFees;
using MAEMS.Application.Features.TuitionFees.Queries.CompareCampusFees;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TuitionFeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TuitionFeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all active tuition fees
    /// </summary>
    /// <returns>List of all active tuition fees</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllActiveTuitionFees()
    {
        var query = new GetAllActiveTuitionFeesQuery();
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Calculate tuition fee for a specific major at a campus
    /// </summary>
    /// <param name="majorName">Name of the major (e.g., "Công nghệ thông tin")</param>
    /// <param name="campusName">Name of the campus (e.g., "Hà Nội")</param>
    /// <param name="region">Region code: "KV1" or "OTHER" (default: "OTHER")</param>
    /// <param name="enrollmentYearId">Optional enrollment year ID</param>
    /// <returns>Calculated tuition fees for different semesters</returns>
    [HttpGet("calculate")]
    public async Task<IActionResult> GetTuitionFeeCalculation(
        [FromQuery] string majorName,
        [FromQuery] string campusName,
        [FromQuery] string region = "OTHER",
        [FromQuery] int? enrollmentYearId = null)
    {
        var query = new GetTuitionFeeCalculationQuery
        {
            MajorName = majorName,
            CampusName = campusName,
            Region = region,
            EnrollmentYearId = enrollmentYearId
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Compare tuition fees across different campuses for a major
    /// </summary>
    /// <param name="majorName">Name of the major (e.g., "Marketing")</param>
    /// <param name="region">Region code: "KV1" or "OTHER" (default: "OTHER")</param>
    /// <returns>Comparison of tuition fees across all campuses</returns>
    [HttpGet("compare-campuses")]
    public async Task<IActionResult> CompareCampusFees(
        [FromQuery] string majorName,
        [FromQuery] string region = "OTHER")
    {
        var query = new CompareCampusFeesQuery
        {
            MajorName = majorName,
            Region = region
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get orientation fee for a campus
    /// </summary>
    /// <param name="campusName">Name of the campus</param>
    /// <param name="region">Region code: "KV1" or "OTHER" (default: "OTHER")</param>
    /// <returns>Orientation fee amount</returns>
    [HttpGet("orientation")]
    public async Task<IActionResult> GetOrientationFee(
        [FromQuery] string campusName,
        [FromQuery] string region = "OTHER")
    {
        var query = new GetTuitionFeeCalculationQuery
        {
            MajorName = "All Majors",
            CampusName = campusName,
            Region = region
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get English preparation fee for a campus
    /// </summary>
    /// <param name="campusName">Name of the campus</param>
    /// <param name="region">Region code: "KV1" or "OTHER" (default: "OTHER")</param>
    /// <returns>English preparation fee per level</returns>
    [HttpGet("english-prep")]
    public async Task<IActionResult> GetEnglishPrepFee(
        [FromQuery] string campusName,
        [FromQuery] string region = "OTHER")
    {
        var query = new GetTuitionFeeCalculationQuery
        {
            MajorName = "All Majors",
            CampusName = campusName,
            Region = region
        };

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
