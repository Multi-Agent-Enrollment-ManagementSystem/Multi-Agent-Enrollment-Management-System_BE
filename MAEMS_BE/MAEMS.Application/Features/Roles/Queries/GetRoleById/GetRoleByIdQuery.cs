using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetRoleById;

public record GetRoleByIdQuery(int RoleId) : IRequest<BaseResponse<RoleDto>>;
