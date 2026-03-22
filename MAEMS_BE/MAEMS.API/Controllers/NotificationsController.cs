using MAEMS.Application.Features.Notifications.Commands.MarkNotificationRead;
using MAEMS.Application.Features.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get notifications for current logged-in user.
    /// UserId is extracted from JWT token.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var result = await _mediator.Send(new GetMyNotificationsQuery { UserId = userId });

        if (!result.Success)
        {
            // Keep consistent with other "me" endpoints (return NotFound on failure)
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mark a notification as read (current user only).
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var result = await _mediator.Send(new MarkNotificationReadCommand
        {
            NotificationId = id,
            UserId = userId
        });

        if (!result.Success)
        {
            if (result.Message == "Notification not found")
                return NotFound(result);

            if (result.Message == "Forbidden")
                return StatusCode(403, result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
