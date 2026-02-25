using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommand : IRequest<BaseResponse<LoginResponseDto>>
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
