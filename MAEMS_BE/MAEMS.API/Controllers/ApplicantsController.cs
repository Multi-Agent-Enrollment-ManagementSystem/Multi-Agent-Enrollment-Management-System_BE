using MAEMS.Application.DTOs.Applicant;
using MAEMS.Application.DTOs.Document;
using MAEMS.Application.Features.Applicants.Commands.CreateApplicant;
using MAEMS.Application.Features.Applicants.Commands.UpdateApplicant;
using MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;
using MAEMS.Application.Features.Applicants.Queries.GetApplicantDocuments;
using MAEMS.Application.Features.Documents.Commands.UploadApplicantDocument;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current user's applicant profile (requires JWT authentication)
    /// </summary>
    /// <returns>Applicant profile</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyApplicant()
    {
        // Lấy userId từ JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var query = new GetMyApplicantQuery(userId);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create applicant profile (requires JWT authentication with role = applicant)
    /// </summary>
    /// <param name="request">Applicant information</param>
    /// <returns>Created applicant profile</returns>
    [HttpPost]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> CreateApplicant([FromBody] CreateApplicantRequestDto request)
    {
        // Lấy userId từ JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        // Tạo command từ request và set userId từ JWT
        var command = new CreateApplicantCommand
        {
            UserId = userId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            HighSchoolName = request.HighSchoolName,
            HighSchoolDistrict = request.HighSchoolDistrict,
            HighSchoolProvince = request.HighSchoolProvince,
            GraduationYear = request.GraduationYear,
            IdIssueNumber = request.IdIssueNumber,
            IdIssueDate = request.IdIssueDate,
            IdIssuePlace = request.IdIssuePlace,
            ContactName = request.ContactName,
            ContactAddress = request.ContactAddress,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            AllowShare = request.AllowShare
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update current user's applicant profile (requires JWT authentication with role = applicant)
    /// </summary>
    /// <param name="request">Fields to update (only provided fields will be updated)</param>
    /// <returns>Updated applicant profile</returns>
    [HttpPatch("me")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> UpdateMyApplicant([FromBody] UpdateApplicantRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var command = new UpdateApplicantCommand
        {
            UserId = userId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            HighSchoolName = request.HighSchoolName,
            HighSchoolDistrict = request.HighSchoolDistrict,
            HighSchoolProvince = request.HighSchoolProvince,
            GraduationYear = request.GraduationYear,
            IdIssueNumber = request.IdIssueNumber,
            IdIssueDate = request.IdIssueDate,
            IdIssuePlace = request.IdIssuePlace,
            ContactName = request.ContactName,
            ContactAddress = request.ContactAddress,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            AllowShare = request.AllowShare
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get applicant by id (requires JWT authentication with roles = officer, admin, qa)
    /// </summary>
    /// <param name="id">Applicant id</param>
    /// <returns>Applicant profile</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "officer,admin,qa")]
    public async Task<IActionResult> GetApplicantById(int id)
    {
        var query = new MAEMS.Application.Features.Applicants.Queries.GetApplicantById.GetApplicantByIdQuery(id);
        var result = await _mediator.Send(query);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    /// <summary>
    /// Get applicant documents by id (requires JWT authentication with roles = officer, admin, applicant)
    /// </summary>
    /// <param name="id">Applicant id</param>
    /// <returns>Applicant documents</returns>
    [HttpGet("{id}/documents")]
    [Authorize(Roles = "officer,admin,applicant,qa")]
    public async Task<IActionResult> GetApplicantDocuments(int id)
    {
        var result = await _mediator.Send(new GetApplicantDocumentsQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Get current user's applicant documents (requires JWT authentication with role = applicant)
    /// </summary>
    /// <returns>Current user's applicant documents</returns>
    [HttpGet("me/documents")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> GetMyApplicantDocuments()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        // Lấy applicantId từ userId trong JWT
        var applicantResult = await _mediator.Send(new GetMyApplicantQuery(userId));
        if (!applicantResult.Success || applicantResult.Data == null)
        {
            return NotFound(new { success = false, message = "Applicant profile not found", errors = new[] { "Please create applicant profile first" } });
        }

        var result = await _mediator.Send(new GetApplicantDocumentsQuery(applicantResult.Data.ApplicantId));
        return Ok(result);
    }

    /// <summary>
    /// Upload document for current user's applicant profile (requires JWT authentication with role = applicant)
    /// </summary>
    /// <param name="request">Document information</param>
    /// <returns>Uploaded document</returns>
    [HttpPost("me/documents")]
    [Authorize(Roles = "applicant")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadApplicantDocument([FromForm] UploadApplicantDocumentRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        // Lấy applicantId từ userId trong JWT
        var applicantQuery = new GetMyApplicantQuery(userId);
        var applicantResult = await _mediator.Send(applicantQuery);
        if (!applicantResult.Success || applicantResult.Data == null)
        {
            return NotFound(new { success = false, message = "Applicant profile not found", errors = new[] { "Please create applicant profile first" } });
        }

        var command = new UploadApplicantDocumentCommand
        {
            ApplicantId = applicantResult.Data.ApplicantId,
            File = request.File,
            UserId = userId
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            if (result.Message == "Applicant not found")
                return NotFound(result);
            if (result.Message == "Forbidden")
                return Forbid();
            return BadRequest(result);
        }

        return Ok(result);
    }
}
