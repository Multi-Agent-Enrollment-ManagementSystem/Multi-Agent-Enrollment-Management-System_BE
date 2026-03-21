using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery(
    int? RoleId = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<UserDto>>>;
