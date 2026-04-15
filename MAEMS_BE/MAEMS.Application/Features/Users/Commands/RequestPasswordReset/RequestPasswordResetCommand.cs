using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.RequestPasswordReset;

public class RequestPasswordResetCommand : IRequest<BaseResponse<string>>
{
    public string Email { get; set; } = string.Empty;
}
