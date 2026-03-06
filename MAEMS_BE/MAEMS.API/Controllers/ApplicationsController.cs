using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Document;
using MAEMS.Application.Features.Applications.Commands.CreateApplication;
using MAEMS.Application.Features.Applications.Commands.PatchApplication;
using MAEMS.Application.Features.Applications.Commands.SubmitApplication;
using  MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;
using MAEMS.Application.Features.Applications.Queries.GetApplicationWithDocuments;
using MAEMS.Application.Features.Applications.Queries.GetMyApplication;
using MAEMS.Application.Features.Applications.Queries.GetMyApplications;
using MAEMS.Application.Features.Applications.Queries.GetMyApplicationWithDocuments;
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
    /// <param name="request">Upload request containing file</param>
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
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyApplications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyApplicationsQuery(userId));
        return Ok(result);
    }
    [HttpGet("all")]
    [Authorize(Roles = "officer,admin")]
    public async Task<IActionResult> GetAllFullApplications()
    {
         var result = await _mediator.Send(new GetAllFullApplicationsQuery());
         return Ok(result);
    }
    
    /// <summary>
    /// Submit an application — changes status from draft to submitted (applicant only).
    /// Triggers Document Verification Agent in the background (fire-and-forget).
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <returns>Updated application</returns>
    [HttpPost("{id}/submit")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> SubmitApplication(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
            }

            var command = new SubmitApplicationCommand
            {
                ApplicationId = id,
                UserId = userId
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                if (result.Message == "Application not found")
                    return NotFound(result);

                if (result.Message == "Forbidden")
                    return StatusCode(403, result);

                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error", errors = new[] { ex.Message } });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "officer,admin")]
    public async Task<IActionResult> GetApplicationWithDocuments(int id)
    {
        var result = await _mediator.Send(new GetApplicationWithDocumentsQuery(id));
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }
    [HttpGet("me/{id}/with-documents")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> GetMyApplicationWithDocuments(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetMyApplicationWithDocumentsQuery(userId, id));

        if (!result.Success)
        {
            if (result.Message == "Application not found")
                return NotFound(result);

            if (result.Message == "Forbidden")
                return StatusCode(403, result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Partially update an application (officer only).
    /// Allows changing <c>Status</c> and/or <c>RequiresReview</c>.
    /// The officer's user id (from JWT) is automatically assigned to <c>AssignedOfficerId</c>.
    /// </summary>
    /// <param name="id">Application ID</param>
    /// <param name="request">Fields to update</param>
    /// <returns>Updated application</returns>
    [HttpPatch("{id}")]
    [Authorize(Roles = "officer")]
    public async Task<IActionResult> PatchApplication(int id, [FromBody] PatchApplicationRequestDto request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int officerUserId))
            {
                return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
            }

            var command = new PatchApplicationCommand
            {
                ApplicationId = id,
                Status = request.Status,
                RequiresReview = request.RequiresReview,
                OfficerUserId = officerUserId
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                if (result.Message == "Application not found")
                    return NotFound(result);

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
