using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, BaseResponse<LoginResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IJwtService _jwtService;

    public LoginUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtService = jwtService;
    }

    public async Task<BaseResponse<LoginResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by username or email
            var user = await _unitOfWork.Users.GetByUsernameAsync(request.UsernameOrEmail);
            
            if (user == null)
            {
                user = await _unitOfWork.Users.GetByEmailAsync(request.UsernameOrEmail);
            }

            // Validate user exists
            if (user == null)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Invalid credentials",
                    new List<string> { "Username/Email or password is incorrect" });
            }

            // Validate user is active
            if (user.IsActive == false)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Account is inactive",
                    new List<string> { "Your account has been deactivated. Please contact support." });
            }

            // Verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            
            if (!isPasswordValid)
            {
                return BaseResponse<LoginResponseDto>.FailureResponse(
                    "Invalid credentials",
                    new List<string> { "Username/Email or password is incorrect" });
            }

            // Get role name if roleId exists
            string? roleName = null;
            if (user.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value);
                roleName = role?.Name;
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roleName);
            var expiresAt = _jwtService.GetTokenExpiration();

            // Map user to LoginUserDto using AutoMapper
            var loginUserDto = _mapper.Map<LoginUserDto>(user);
            loginUserDto.Role = roleName; // Set role after mapping

            // Create response
            var loginResponse = new LoginResponseDto
            {
                Token = token,
                User = loginUserDto,
                ExpiresAt = expiresAt
            };

            return BaseResponse<LoginResponseDto>.SuccessResponse(loginResponse, "Login successful");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };
            
            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<LoginResponseDto>.FailureResponse(
                "Login failed",
                errors);
        }
    }
}
