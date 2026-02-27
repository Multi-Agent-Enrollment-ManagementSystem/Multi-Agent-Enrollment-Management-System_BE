using MAEMS.Application.Features.Users.Commands.GoogleLogin;
using MAEMS.Application.Features.Users.Commands.LoginUser;
using MAEMS.Application.Features.Users.Commands.RefreshToken;
using MAEMS.Application.Features.Users.Commands.RegisterUser;
using MAEMS.Application.Features.Users.Commands.VerifyEmail;
using MAEMS.Application.Features.Users.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public UsersController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="command">Registration details (Username, Email, Password)</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    /// <param name="command">Login credentials (UsernameOrEmail, Password)</param>
    /// <returns>JWT access token, refresh token and user information</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with Google account
    /// </summary>
    /// <param name="command">Google ID Token from Google Sign-In</param>
    /// <returns>JWT access token, refresh token and user information</returns>
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="command">Refresh token request (AccessToken, RefreshToken)</param>
    /// <returns>New JWT access token and refresh token</returns>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get user profile (requires JWT authentication)
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        // Get user ID from JWT token claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { success = false, message = "Invalid token", errors = new[] { "User ID not found in token" } });
        }

        var query = new GetUserProfileQuery(userId);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify user email with verification token
    /// </summary>
    /// <param name="token">Email verification token</param>
    /// <returns>Redirects to frontend URL</returns>
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "https://google.com";

        if (string.IsNullOrWhiteSpace(token))
        {
            return Redirect(frontendUrl);
        }

        var command = new VerifyEmailCommand(token);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return Redirect(frontendUrl);
        }

        // Redirect to login page on success
        return Redirect($"{frontendUrl}");
    }
}
