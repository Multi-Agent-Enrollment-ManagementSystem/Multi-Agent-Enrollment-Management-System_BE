using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Document;
using MAEMS.Application.Features.Applications.Commands.CreateApplication;
using MAEMS.Application.Features.Documents.Commands.UploadDocument;
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
                AdmissionTypeId = request.AdmissionTypeId
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

    /// <summary>
    /// Upload document for application (requires JWT authentication with role = applicant)
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Upload request containing document type and file</param>
    /// <returns>Uploaded document information</returns>
    [HttpPost("{id}/documents")]
    [Authorize(Roles = "applicant")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(int id, [FromForm] UploadDocumentRequestDto request)
    {
        try
        {
            // Validate that the application belongs to the current user
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
            }

            // Get applicant from userId
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(userId);
            if (applicant == null)
            {
                return BadRequest(new { success = false, message = "Applicant profile not found", errors = new[] { "Please create applicant profile first" } });
            }

            // Check if application belongs to this applicant
            var application = await _unitOfWork.Applications.GetByIdAsync(id);
            if (application == null)
            {
                return NotFound(new { success = false, message = "Application not found", errors = new[] { $"Application with ID {id} does not exist" } });
            }

            if (application.ApplicantId != applicant.ApplicantId)
            {
                return Forbid();
            }

            var command = new UploadDocumentCommand
            {
                ApplicationId = id,
                DocumentType = request.DocumentType,
                File = request.File
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