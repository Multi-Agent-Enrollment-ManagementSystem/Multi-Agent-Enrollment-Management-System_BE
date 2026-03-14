using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetUserDetailById;

public record GetUserDetailByIdQuery(int UserId) : IRequest<BaseResponse<UserDetailDto>>;
