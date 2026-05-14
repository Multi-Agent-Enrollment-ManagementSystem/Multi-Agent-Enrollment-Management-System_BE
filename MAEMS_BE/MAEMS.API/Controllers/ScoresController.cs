using MAEMS.Application.DTOs.Score;
using MAEMS.Application.Features.Scores.Commands.UpdateScore;
using MAEMS.Application.Features.Scores.Queries.GetScoreByApplicantId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScoresController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get score by applicant ID
    /// </summary>
    /// <param name="applicantId">Applicant ID</param>
    /// <returns>Score profile</returns>
    [HttpGet("applicant/{applicantId}")]
    //[Authorize(Roles = "officer")]
    public async Task<IActionResult> GetScoreByApplicantId([FromRoute] int applicantId)
    {
        var query = new GetScoreByApplicantIdQuery(applicantId);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update or create score for an applicant
    /// </summary>
    /// <param name="applicantId">Applicant ID</param>
    /// <param name="command">Score data to update</param>
    /// <returns>Updated score profile</returns>
    [HttpPut("applicant/{applicantId}")]
    //[Authorize(Roles = "officer")]
    public async Task<IActionResult> UpdateScore([FromRoute] int applicantId, [FromBody] UpdateScoreCommand command)
    {
        command.ApplicantId = applicantId;

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
