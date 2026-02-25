using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetAllRoles;

public record GetAllRolesQuery : IRequest<BaseResponse<IEnumerable<RoleDto>>>;
