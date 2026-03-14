using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.PatchUserIsActive;

public class PatchUserIsActiveCommandHandler : IRequestHandler<PatchUserIsActiveCommand, BaseResponse<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchUserIsActiveCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserDto>> Handle(PatchUserIsActiveCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return BaseResponse<UserDto>.FailureResponse(
                    $"User with ID {request.UserId} not found",
                    new List<string> { "User not found" });
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId.Value);
                if (role == null)
                {
                    return BaseResponse<UserDto>.FailureResponse(
                        $"Role with ID {request.RoleId.Value} not found",
                        new List<string> { "Invalid roleId" });
                }

                user.RoleId = request.RoleId.Value;
            }

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<UserDto>(user);

            if (user.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value);
                dto.RoleName = role?.Name;
            }

            return BaseResponse<UserDto>.SuccessResponse(dto, "User updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<UserDto>.FailureResponse(
                "Error updating user",
                new List<string> { ex.Message });
        }
    }
}
