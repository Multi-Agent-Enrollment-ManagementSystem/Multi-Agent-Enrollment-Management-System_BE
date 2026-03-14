using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.PatchUserIsActive;

public class PatchUserIsActiveCommand : IRequest<BaseResponse<UserDto>>
{
    public int UserId { get; set; }
    public bool? IsActive { get; set; }
    public int? RoleId { get; set; }
}
