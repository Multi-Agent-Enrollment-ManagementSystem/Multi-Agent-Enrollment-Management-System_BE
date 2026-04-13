using MAEMS.Application.Features.Reports.Queries.GetApplicationStatusCounts;
using MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByAssignedOfficer;
using MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByCampus;
using MAEMS.Application.Features.Reports.Queries.GetMyAssignedApplicationsOverview;
using MAEMS.Application.Features.Reports.Queries.GetNonDraftApplicationsCountByProgramInCampus;
using MAEMS.Application.Features.Reports.Queries.GetPaidRevenueByQuarter;
using MAEMS.Application.Features.Reports.Queries.GetReportSummary;
using MAEMS.Application.Features.Reports.Queries.GetWeeklySubmittedApplicationsCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ReportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Summary report.
    /// Returns: NumApplicant, NumApplication, NumPaymentNeedCheck, NumProgram.
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReportSummaryQuery(), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Count applications with status != draft, grouped by week (based on submitted_at).
    /// Optional query params: from, to.
    /// </summary>
    [HttpGet("applications/weekly")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetWeeklySubmittedApplicationsCount(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWeeklySubmittedApplicationsCountQuery(from, to), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Distribution of applications grouped by campus (Status != draft).
    /// </summary>
    [HttpGet("applications/by-campus")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetApplicationsCountByCampus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationsCountByCampusQuery(), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Distribution of applications (Status != draft) grouped by program within a given campus.
    /// </summary>
    [HttpGet("applications/by-campus/{campusId:int}/programs")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetNonDraftApplicationsCountByProgramInCampus(
        [FromRoute] int campusId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNonDraftApplicationsCountByProgramInCampusQuery(campusId), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Application counts: approved, rejected, and pending (status != draft/approved/rejected).
    /// </summary>
    [HttpGet("applications/status-counts")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetApplicationStatusCounts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationStatusCountsQuery(), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Distribution of applications grouped by assigned_officer_id.
    /// </summary>
    [HttpGet("applications/by-assigned-officer")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetApplicationsCountByAssignedOfficer(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApplicationsCountByAssignedOfficerQuery(), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Officer: load all applications assigned to current officer (from JWT user_id) and return counts.
    /// </summary>
    [HttpGet("officer/me/applications")]
    [Authorize(Roles = "officer")]
    public async Task<IActionResult> GetMyAssignedApplicationsOverview(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var officerUserId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var result = await _mediator.Send(new GetMyAssignedApplicationsOverviewQuery(officerUserId), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Revenue report: total Amount of payments with status = Paid, grouped by quarter of a given year.
    /// Query params: year (default = current year)
    /// </summary>
    [HttpGet("payments/revenue/quarterly")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetPaidRevenueByQuarter(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var result = await _mediator.Send(new GetPaidRevenueByQuarterQuery(targetYear), cancellationToken);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
