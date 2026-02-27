using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.GoogleLogin;

public class GoogleLoginCommand : IRequest<BaseResponse<LoginResponseDto>>
{
    public string IdToken { get; set; } = string.Empty;
}
