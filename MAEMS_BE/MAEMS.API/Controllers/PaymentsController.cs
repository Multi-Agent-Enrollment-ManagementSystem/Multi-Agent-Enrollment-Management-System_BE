using MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;
using MAEMS.Application.Features.Payments.Commands.SepayWebhook;
using MAEMS.Application.Features.Payments.Queries.GetAllPayments;
using MAEMS.Application.Features.Payments.Queries.GetMyPayments;
using MAEMS.Application.Features.Payments.Queries.GetPaymentByTransactionId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get my payments (applicant only). Applicant is resolved from JWT user_id (NameIdentifier).
    /// </summary>
    /// <param name="status">Optional status filter (matches paymentStatus)</param>
    /// <param name="transactionId">Optional transactionId filter</param>
    /// <param name="paidFrom">Optional paid from (inclusive)</param>
    /// <param name="paidTo">Optional paid to (inclusive)</param>
    /// <param name="sortBy">Sort field (allowed: paymentId [default], amount, transactionId, paymentStatus, paidAt)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    [HttpGet("me")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> GetMyPayments(
        [FromQuery] string? status,
        [FromQuery] string? transactionId,
        [FromQuery] DateTime? paidFrom,
        [FromQuery] DateTime? paidTo,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var result = await _mediator.Send(new GetMyPaymentsQuery(
            userId,
            status,
            transactionId,
            paidFrom,
            paidTo,
            sortBy,
            sortDesc,
            pageNumber,
            pageSize));

        if (!result.Success)
        {
            if (result.Message == "Applicant profile not found")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all payments (admin only) with SQL-level sort & paging
    /// </summary>
    /// <param name="status">Optional status filter (matches paymentStatus)</param>
    /// <param name="transactionId">Optional transactionId filter</param>
    /// <param name="paidFrom">Optional paid from (inclusive)</param>
    /// <param name="paidTo">Optional paid to (inclusive)</param>
    /// <param name="sortBy">Sort field (allowed: paymentId [default], amount, transactionId, paymentStatus, paidAt)</param>
    /// <param name="sortDesc">Sort descending (default false)</param>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20, max 100)</param>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllPayments(
        [FromQuery] string? status,
        [FromQuery] string? transactionId,
        [FromQuery] DateTime? paidFrom,
        [FromQuery] DateTime? paidTo,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllPaymentsQuery(
            status,
            transactionId,
            paidFrom,
            paidTo,
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
    /// Get payment by transactionId (admin or owner applicant)
    /// </summary>
    /// <param name="transactionId">Exact transactionId</param>
    [HttpGet("by-transaction/{transactionId}")]
    [Authorize(Roles = "admin,applicant")]
    public async Task<IActionResult> GetByTransactionId([FromRoute] string transactionId)
    {
        var result = await _mediator.Send(new GetPaymentByTransactionIdQuery(transactionId));

        if (!result.Success)
        {
            if (result.Message == "Payment not found")
                return NotFound(result);

            return BadRequest(result);
        }

        // If caller is applicant, only allow accessing their own payment
        if (User.IsInRole("applicant"))
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
            }

            // Resolve applicantId from userId
            var applicantResult = await _mediator.Send(new GetMyApplicantQuery(userId));
            if (!applicantResult.Success || applicantResult.Data == null)
            {
                return NotFound(new { success = false, message = "Applicant profile not found", errors = new[] { "Please create applicant profile first" } });
            }

            var paymentApplicantId = result.Data?.ApplicantId;
            if (!paymentApplicantId.HasValue || paymentApplicantId.Value != applicantResult.Data.ApplicantId)
            {
                return Forbid();
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// SePay webhook endpoint.
    /// Note: usually called by SePay servers (no JWT). Keep anonymous unless you add signature verification.
    /// </summary>
    [HttpPost("sepay-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Message == "Payment not found")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
