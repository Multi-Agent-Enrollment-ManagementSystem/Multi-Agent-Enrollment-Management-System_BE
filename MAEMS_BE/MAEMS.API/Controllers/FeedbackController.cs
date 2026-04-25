using MAEMS.Application.Features.Feedback.Commands.SubmitFeedback;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "qa,admin")]
    public async Task<IActionResult> GetAllFeedbacks([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var response = await _mediator.Send(new MAEMS.Application.Features.Feedback.Queries.GetAllFeedbacks.GetAllFeedbacksQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "qa")]
    public async Task<IActionResult> SubmitFeedback([FromBody] SubmitFeedbackCommand command)
    {
        // Extract User ID from token
        if (int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
        {
            command.UserId = userId;
        }

        var response = await _mediator.Send(command);
        return Ok(response);
    }
}