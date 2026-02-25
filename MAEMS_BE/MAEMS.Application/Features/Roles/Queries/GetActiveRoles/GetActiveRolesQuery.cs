using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetActiveRoles;

public record GetActiveRolesQuery : IRequest<BaseResponse<IEnumerable<RoleDto>>>;
