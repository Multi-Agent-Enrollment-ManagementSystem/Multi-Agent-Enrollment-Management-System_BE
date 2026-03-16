using MAEMS.Application.DTOs.Chat;
using MAEMS.Application.Features.Chat.Commands.AskChatBox;
using MAEMS.Application.Features.Chat.Queries.GetChatHistory;
using MAEMS.Application.Interfaces;
using MAEMS.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

/// <summary>
/// Controller để xử lý chatbox Q&A cho thí sinh
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatBoxController : ControllerBase
{
    private readonly IChatBoxAgent _chatBoxAgent;
    private readonly ILlmChatLogRepository _chatLogRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ChatBoxController> _logger;

    public ChatBoxController(
        IChatBoxAgent chatBoxAgent,
        ILlmChatLogRepository chatLogRepository,
        IMediator mediator,
        ILogger<ChatBoxController> logger)
    {
        _chatBoxAgent = chatBoxAgent;
        _chatLogRepository = chatLogRepository;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gửi câu hỏi tới chatbox AI
    /// </summary>
    /// <param name="request">Request chứa câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trả lời từ AI</returns>
    [HttpPost("ask")]
    [Produces("application/json")]
    public async Task<ActionResult<ChatBoxResponseDto>> AskQuestion(
        [FromBody] AskChatBoxRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID in claims");
                return Unauthorized("User ID not found in token");
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { message = "Question cannot be empty" });
            }

            if (request.Question.Length > 5000)
            {
                return BadRequest(new { message = "Question is too long (max 5000 characters)" });
            }

            _logger.LogInformation("User {UserId} asked question: {Question}", userId, request.Question);

            // Call ChatBoxAgent to get response
            var response = await _chatBoxAgent.RespondAsync(userId, request.Question, cancellationToken);

            // Get the saved chat log
            var chatLogs = await _chatLogRepository.GetByUserIdAsync(userId, 1, 1, cancellationToken);
            var lastChat = chatLogs.FirstOrDefault();

            return Ok(new ChatBoxResponseDto
            {
                ChatId = lastChat?.ChatId ?? 0,
                Question = request.Question,
                Answer = response,
                CreatedAt = DateTime.Now
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Request timeout. Please try again." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API error - Check Gemini API status");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "AI service is temporarily unavailable. Check your API key and network connection." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid configuration - {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "AI service configuration error. Please contact administrator." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat question - {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while processing your question" });
        }
    }

    /// <summary>
    /// Lấy lịch sử chat của user
    /// </summary>
    /// <param name="pageNumber">Số trang (default 1)</param>
    /// <param name="pageSize">Số messages mỗi trang (default 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Danh sách chat history</returns>
    [HttpGet("history")]
    [Produces("application/json")]
    public async Task<ActionResult<ChatHistoryResponseDto>> GetChatHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID in claims");
                return Unauthorized("User ID not found in token");
            }

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Retrieving chat history for user {UserId}, page {PageNumber}, size {PageSize}",
                userId, pageNumber, pageSize);

            // Get total count
            var totalCount = await _chatLogRepository.GetCountByUserIdAsync(userId, cancellationToken);

            // Get paginated messages
            var chatLogs = await _chatLogRepository.GetByUserIdAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            // Map to DTO
            var messages = chatLogs.Select(log => new ChatMessageDto
            {
                ChatId = log.ChatId,
                Question = log.UserQuery ?? "",
                Answer = log.LlmResponse ?? "",
                CreatedAt = log.CreatedAt ?? DateTime.UtcNow
            }).ToList();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new ChatHistoryResponseDto
            {
                Messages = messages,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving chat history" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> Health()
    {
        return Ok(new { status = "ChatBox service is running" });
    }
}
