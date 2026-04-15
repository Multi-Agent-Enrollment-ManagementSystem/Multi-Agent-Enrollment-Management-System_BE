using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordCommand : IRequest<BaseResponse<string>>
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
