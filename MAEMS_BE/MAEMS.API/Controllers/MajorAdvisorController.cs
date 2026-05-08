using MAEMS.Application.DTOs.MajorAdvisor;
using MAEMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MAEMS.API.Controllers;

/// <summary>
/// Major Advisor - Public endpoint to analyze academic documents and recommend university majors.
/// No authentication required - designed for prospective students to get guidance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MajorAdvisorController : ControllerBase
{
    private readonly IMajorAdvisorAgent _agent;
    private readonly ILogger<MajorAdvisorController> _logger;

    public MajorAdvisorController(
        IMajorAdvisorAgent agent,
        ILogger<MajorAdvisorController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Analyze an academic document (học bạ THPT or kết quả ĐGNL) and get major recommendations.
    /// </summary>
    /// <param name="request">Document upload request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Major recommendations with detailed reasoning</returns>
    /// <response code="200">Analysis successful - returns recommendations</response>
    /// <response code="400">Invalid file or unsupported format</response>
    /// <response code="500">Server error during analysis</response>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)] // 20MB max
    [ProducesResponseType(typeof(MajorAdvisorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MajorAdvisorResult>> Analyze(
        [FromForm] AnalyzeDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "Vui lòng tải lên file học bạ hoặc kết quả thi ĐGNL." });
        }

        // Validate file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                error = $"Định dạng file không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}"
            });
        }

        // Validate file size (20MB)
        if (request.File.Length > 20_000_000)
        {
            return BadRequest(new { error = "Kích thước file vượt quá 20MB." });
        }

        try
        {
            _logger.LogInformation(
                "MajorAdvisorController: Received file '{FileName}' ({Size} bytes) from {IP}",
                request.File.FileName, request.File.Length, HttpContext.Connection.RemoteIpAddress);

            var result = await _agent.AnalyzeAndRecommendAsync(request.File, cancellationToken);

            if (result.Result != "passed")
            {
                return BadRequest(new { error = result.Summary ?? "Không thể phân tích tài liệu." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MajorAdvisorController: Error analyzing file '{FileName}'", request.File.FileName);
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi phân tích tài liệu. Vui lòng thử lại sau." });
        }
    }

    /// <summary>
    /// Health check endpoint for Major Advisor service
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "MajorAdvisor",
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
