using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using BCrypt.Net;

namespace MAEMS.Application.Features.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, BaseResponse<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RegisterUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate username exists
            if (await _unitOfWork.Users.IsUsernameExistsAsync(request.Username))
            {
                return BaseResponse<UserDto>.FailureResponse(
                    "Username already exists",
                    new List<string> { "Username is already taken" });
            }

            // Validate email exists
            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
            {
                return BaseResponse<UserDto>.FailureResponse(
                    "Email already exists",
                    new List<string> { "Email is already registered" });
            }

            // Hash password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user entity
            var user = new MAEMS.Domain.Entities.User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleId = 4, // Default role ID as specified
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                IsActive = true
            };

            // Save to database through repository
            var savedUser = await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Map to DTO
            var userDto = _mapper.Map<UserDto>(savedUser);

            return BaseResponse<UserDto>.SuccessResponse(userDto, "User registered successfully");
        }
        catch (Exception ex)
        {
            // Log detailed error information including inner exception
            var errors = new List<string> { ex.Message };
            
            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
                
                if (ex.InnerException.InnerException != null)
                {
                    errors.Add($"Inner Inner Exception: {ex.InnerException.InnerException.Message}");
                }
            }

            // Add stack trace for debugging (remove in production)
            errors.Add($"Stack Trace: {ex.StackTrace}");

            return BaseResponse<UserDto>.FailureResponse(
                "Failed to register user",
                errors);
        }
    }
}
