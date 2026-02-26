using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<BaseResponse<bool>>;
