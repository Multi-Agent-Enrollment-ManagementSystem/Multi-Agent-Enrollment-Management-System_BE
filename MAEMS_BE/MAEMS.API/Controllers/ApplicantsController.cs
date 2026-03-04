using MAEMS.Application.DTOs.Applicant;
using MAEMS.Application.Features.Applicants.Commands.CreateApplicant;
using MAEMS.Application.Features.Applicants.Commands.UpdateApplicant;
using MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;
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
        // Lấy userId từ claim (giả sử claim name là "userId")
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetMyApplicantQuery(userId));
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
    /// Get applicant by id
    /// </summary>
    /// <param name="id">Applicant id</param>
    /// <returns>Applicant profile</returns>
    [HttpGet("{id}")]
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
}
