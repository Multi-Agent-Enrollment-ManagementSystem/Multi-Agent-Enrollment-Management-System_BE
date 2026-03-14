using AutoMapper;
using BCrypt.Net;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.CreateUserByAdmin;

public class CreateUserByAdminCommandHandler : IRequestHandler<CreateUserByAdminCommand, BaseResponse<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserByAdminCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserDto>> Handle(CreateUserByAdminCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (await _unitOfWork.Users.IsUsernameExistsAsync(request.Username))
            {
                return BaseResponse<UserDto>.FailureResponse(
                    "Username already exists",
                    new List<string> { "Username is already taken" });
            }

            if (await _unitOfWork.Users.IsEmailExistsAsync(request.Email))
            {
                return BaseResponse<UserDto>.FailureResponse(
                    "Email already exists",
                    new List<string> { "Email is already registered" });
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId);
            if (role == null)
            {
                return BaseResponse<UserDto>.FailureResponse(
                    $"Role with ID {request.RoleId} not found",
                    new List<string> { "Invalid roleId" });
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new MAEMS.Domain.Entities.User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                IsActive = true
            };

            var savedUser = await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<UserDto>(savedUser);
            dto.RoleName = role.Name;

            return BaseResponse<UserDto>.SuccessResponse(dto, "User created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<UserDto>.FailureResponse(
                "Failed to create user",
                new List<string> { ex.Message });
        }
    }
}
