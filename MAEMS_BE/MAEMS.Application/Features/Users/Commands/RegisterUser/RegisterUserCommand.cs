using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.RegisterUser;

public record RegisterUserCommand(string Username, string Email, string Password) : IRequest<BaseResponse<UserDto>>;
