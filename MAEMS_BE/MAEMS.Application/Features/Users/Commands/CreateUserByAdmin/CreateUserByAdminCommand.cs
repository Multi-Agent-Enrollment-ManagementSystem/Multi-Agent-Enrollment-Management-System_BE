using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.CreateUserByAdmin;

public class CreateUserByAdminCommand : IRequest<BaseResponse<UserDto>>
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
}
