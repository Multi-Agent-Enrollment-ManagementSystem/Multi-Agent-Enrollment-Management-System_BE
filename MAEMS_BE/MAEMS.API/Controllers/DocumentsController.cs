using System.Security.Claims;
using MAEMS.Application.Features.Documents.Commands.DeleteDocument;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Delete document by id (requires JWT authentication with role = applicant). Only the author (owner) can delete.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "applicant")]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var result = await _mediator.Send(new DeleteDocumentCommand(id, userId));

        if (!result.Success)
        {
            if (result.Message == "Document not found") return NotFound(result);
            if (result.Message == "Forbidden") return Forbid();
            return BadRequest(result);
        }

        return Ok(result);
    }
}
