using System.Security.Claims;
using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, BaseResponse<LoginResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtService = jwtService;
    }

    public async Task<BaseResponse<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate the access token (without checking expiration)
            var principal = _jwtService.ValidateToken(request.AccessToken);

            if (principal == null)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Invalid token",
                    new List<string> { "The provided access token is invalid" });
            }

            // Extract user ID from the token
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Invalid token",
                    new List<string> { "User ID not found in token" });
            }

            // Get user from database
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "User not found",
                    new List<string> { "The user associated with this token no longer exists" });
            }

            // Validate user is active
            if (user.IsActive == false)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Account is inactive",
                    new List<string> { "Your account has been deactivated. Please contact support." });
            }

            // Get role name if roleId exists
            string? roleName = null;
            if (user.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value);
                roleName = role?.Name;
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roleName);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var accessTokenExpiresAt = _jwtService.GetTokenExpiration();
            var refreshTokenExpiresAt = _jwtService.GetRefreshTokenExpiration();

            // Map user to LoginUserDto using AutoMapper
            var loginUserDto = _mapper.Map<LoginUserDto>(user);
            loginUserDto.Role = roleName;

            // Create response
            var loginResponse = new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = loginUserDto,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };

            return BaseResponse<LoginResponseDto>.SuccessResponse(loginResponse, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };
            
            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<LoginResponseDto>.FailureResponse(
                "Token refresh failed",
                errors);
        }
    }
}
