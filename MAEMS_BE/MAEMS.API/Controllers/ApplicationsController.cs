using MAEMS.Application.DTOs.Application;
using MAEMS.Application.Features.Applications.Commands.CreateApplication;
using MAEMS.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationsController(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Create application (requires JWT authentication with role = applicant)
    /// </summary>
    /// <param name="request">Application information</param>
    /// <returns>Created application</returns>
    [HttpPost]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequestDto request)
    {
        try
        {
            // Lấy userId từ JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
            }

            // Lấy applicant từ userId
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(userId);

            if (applicant == null)
            {
                return BadRequest(new { success = false, message = "Applicant profile not found", errors = new[] { "Please create applicant profile first" } });
            }

            // Tạo command với applicantId từ database
            var command = new CreateApplicationCommand
            {
                ApplicantId = applicant.ApplicantId,
                ProgramId = request.ProgramId,
                EnrollmentYearId = request.EnrollmentYearId,
                CampusId = request.CampusId,
                AdmissionTypeId = request.AdmissionTypeId,
                Notes = request.Notes
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error", errors = new[] { ex.Message } });
        }
    }
}