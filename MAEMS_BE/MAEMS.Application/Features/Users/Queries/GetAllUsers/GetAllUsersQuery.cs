using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery(int? RoleId = null) : IRequest<BaseResponse<IEnumerable<UserDto>>>;
