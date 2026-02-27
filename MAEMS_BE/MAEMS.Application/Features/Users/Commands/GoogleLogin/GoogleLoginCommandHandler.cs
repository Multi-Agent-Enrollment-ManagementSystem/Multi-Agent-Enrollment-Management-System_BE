using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, BaseResponse<LoginResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public GoogleLoginCommandHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper, 
        IJwtService jwtService,
        IFirebaseAuthService firebaseAuthService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtService = jwtService;
        _firebaseAuthService = firebaseAuthService;
    }

    public async Task<BaseResponse<LoginResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate Google ID Token
            var (isValid, email, name) = await _firebaseAuthService.ValidateGoogleTokenAsync(request.IdToken);

            if (!isValid || string.IsNullOrEmpty(email))
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Invalid Google token",
                    new List<string> { "Failed to validate Google authentication" });
            }

            // Check if user exists
            var user = await _unitOfWork.Users.GetByEmailAsync(email);

            // If user doesn't exist, create new user
            if (user == null)
            {
                // Generate username from email (before @)
                var username = email.Split('@')[0];
                
                // Check if username already exists, if so append random string
                var existingUser = await _unitOfWork.Users.GetByUsernameAsync(username);
                if (existingUser != null)
                {
                    username = $"{username}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                // Create new user with Google authentication
                user = new MAEMS.Domain.Entities.User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for Google users
                    IsActive = true,
                    CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                    RoleId = 4 // Default role ID (same as register)
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
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

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roleName);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var accessTokenExpiresAt = _jwtService.GetTokenExpiration();
            var refreshTokenExpiresAt = _jwtService.GetRefreshTokenExpiration();

            // Map user to LoginUserDto
            var loginUserDto = _mapper.Map<LoginUserDto>(user);
            loginUserDto.Role = roleName;

            // Create response
            var loginResponse = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = loginUserDto,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };

            return BaseResponse<LoginResponseDto>.SuccessResponse(loginResponse, "Google login successful");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };
            
            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<LoginResponseDto>.FailureResponse(
                "Google login failed",
                errors);
        }
    }
}
