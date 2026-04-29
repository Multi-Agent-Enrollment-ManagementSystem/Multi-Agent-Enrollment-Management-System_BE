using MAEMS.Application.Features.RegisterEvents.Commands.CreateRegisterEvent;
using MAEMS.Application.Features.RegisterEvents.Queries.GetRegisterEventsByArticleId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterEventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RegisterEventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Public: Create register event for an article
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRegisterEvent([FromBody] CreateRegisterEventCommand request)
    {
        var result = await _mediator.Send(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Admin/Officer: Get register events by article id
    /// </summary>
    [HttpGet("article/{articleId:int}")]
    [Authorize(Roles = "officer")]
    public async Task<IActionResult> GetRegisterEventsByArticleId([FromRoute] int articleId)
    {
        var result = await _mediator.Send(new GetRegisterEventsByArticleIdQuery { ArticleId = articleId });
        
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}