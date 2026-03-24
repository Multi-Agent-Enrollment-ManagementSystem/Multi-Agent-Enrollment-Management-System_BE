using MAEMS.Application.DTOs.Article;
using MAEMS.Application.Features.Articles.Commands.CreateArticle;
using MAEMS.Application.Features.Articles.Commands.PatchArticle;
using MAEMS.Application.Features.Articles.Queries.GetArticleById;
using MAEMS.Application.Features.Articles.Queries.GetArticlesBasic;
using MAEMS.Application.Features.Articles.Queries.GetPublishedArticlesBasic;
using MAEMS.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorageService;

    public ArticlesController(IMediator mediator, IFileStorageService fileStorageService)
    {
        _mediator = mediator;
        _fileStorageService = fileStorageService;
    }

    public class UploadArticleImageRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = default!;
    }

    /// <summary>
    /// Upload article image (admin/officer). Stores in Firebase Storage and returns public URL.
    /// </summary>
    [HttpPost("upload-image")]
    [Authorize(Roles = "admin,officer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadArticleImage([FromForm] UploadArticleImageRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        if (request.File == null || request.File.Length == 0)
            return BadRequest(new { success = false, message = "File is required", errors = new[] { "File is required" } });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { success = false, message = "Invalid file type", errors = new[] { $"Allowed: {string.Join(", ", allowedExtensions)}" } });

        const long maxSize = 5 * 1024 * 1024; // 5MB
        if (request.File.Length > maxSize)
            return BadRequest(new { success = false, message = "File too large", errors = new[] { "Max size is 5MB" } });

        string url;
        using (var stream = request.File.OpenReadStream())
        {
            // store under user folder for traceability
            url = await _fileStorageService.UploadFileAsync(stream, request.File.FileName, $"articles/{userId}/images");
        }

        return Ok(new UploadArticleImageResponseDto { Url = url });
    }

    /// <summary>
    /// Create article (admin/officer). author_id will be taken from JWT (NameIdentifier).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin,officer")]
    public async Task<IActionResult> CreateArticle([FromBody] CreateArticleCommand request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        // Always trust JWT for author
        request.AuthorId = userId;

        var result = await _mediator.Send(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Public: Get published articles (basic fields) with SQL-level paging/sort/filter.
    /// </summary>
    [HttpGet("publish/basic")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedArticlesBasic(
        [FromQuery] string? searchTitle,
        [FromQuery] string? sortBy = "updatedAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPublishedArticlesBasicQuery
        {
            SearchTitle = searchTitle,
            SortBy = sortBy,
            SortDesc = sortDesc,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Admin/Officer: Get all articles (basic fields) with SQL-level paging/sort/filter.
    /// </summary>
    [HttpGet("basic")]
    [Authorize(Roles = "admin,officer")]
    public async Task<IActionResult> GetArticlesBasic(
        [FromQuery] string? searchTitle,
        [FromQuery] string? status,
        [FromQuery] string? sortBy = "updatedAt",
        [FromQuery] bool sortDesc = true,
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetArticlesBasicQuery
        {
            SearchTitle = searchTitle,
            Status = status,
            SortBy = sortBy,
            SortDesc = sortDesc,
            PageNumber = pageNumber,
            PageSize = pageSize
        });

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Get article by id.
    /// - AllowAnonymous only when article status = publish
    /// - Otherwise requires admin/officer
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetArticleById([FromRoute] int id)
    {
        var result = await _mediator.Send(new GetArticleByIdQuery { ArticleId = id });

        if (!result.Success)
            return NotFound(result);

        var dto = result.Data;
        var isPublished = dto?.Status != null && dto.Status.Equals("publish", StringComparison.OrdinalIgnoreCase);

        if (!isPublished)
        {
            var isStaff = (User?.Identity?.IsAuthenticated ?? false)
                && (User.IsInRole("admin") || User.IsInRole("officer"));

            if (!isStaff)
                return Unauthorized(new { success = false, message = "Admin/Officer authorization required", errors = new[] { "Not authorized" } });
        }

        return Ok(result);
    }

    /// <summary>
    /// Admin/Officer: Patch article fields (title, content, thumbnail, status).
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "admin,officer")]
    public async Task<IActionResult> PatchArticle([FromRoute] int id, [FromBody] PatchArticleCommand request)
    {
        request.ArticleId = id;

        var result = await _mediator.Send(request);

        if (!result.Success)
        {
            if (result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
