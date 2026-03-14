using MAEMS.Application.Features.Users.Commands.GoogleLogin;
using MAEMS.Application.Features.Users.Commands.LoginUser;
using MAEMS.Application.Features.Users.Commands.RefreshToken;
using MAEMS.Application.Features.Users.Commands.RegisterUser;
using MAEMS.Application.Features.Users.Commands.VerifyEmail;
using MAEMS.Application.Features.Users.Queries.GetUserProfile;
using MAEMS.Application.Features.Users.Queries.GetAllUsers;
using MAEMS.Application.Features.Users.Commands.CreateUserByAdmin;
using MAEMS.Application.Features.Users.Commands.PatchUserIsActive;
using MAEMS.Application.Features.Users.Queries.GetUserDetailById;
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

    /// <summary>
    /// Get all users (requires JWT authentication with role = admin)
    /// </summary>
    /// <param name="roleId">Optional role id filter</param>
    /// <returns>List of users</returns>
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int? roleId)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(roleId));

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new user (admin only). This does not require email verification and sets IsActive = true.
    /// </summary>
    /// <param name="command">User details (Username, Email, Password, RoleId)</param>
    /// <returns>Created user information</returns>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    public class PatchUserRequest
    {
        public bool? IsActive { get; set; }
        public int? RoleId { get; set; }
    }

    /// <summary>
    /// Update user's active status and/or role (admin only)
    /// </summary>
    /// <param name="id">User id</param>
    /// <param name="request">Fields to update</param>
    /// <returns>Updated user information</returns>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> PatchUser([FromRoute] int id, [FromBody] PatchUserRequest request)
    {
        var result = await _mediator.Send(new PatchUserIsActiveCommand
        {
            UserId = id,
            IsActive = request.IsActive,
            RoleId = request.RoleId
        });

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get user by id (admin only). If user's RoleId = 4, include applicant profile.
    /// </summary>
    /// <param name="id">User id</param>
    /// <returns>User details</returns>
    [HttpGet("by-id/{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
        var result = await _mediator.Send(new GetUserDetailByIdQuery(id));

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
