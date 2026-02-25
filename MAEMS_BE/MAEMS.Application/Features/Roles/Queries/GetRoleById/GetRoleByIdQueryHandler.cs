using AutoMapper;
using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetRoleById;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, BaseResponse<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetRoleByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId);

            if (role == null)
            {
                return BaseResponse<RoleDto>.FailureResponse(
                    $"Role with ID {request.RoleId} not found",
                    new List<string> { "Role not found" }
                );
            }

            var roleDto = _mapper.Map<RoleDto>(role);
            return BaseResponse<RoleDto>.SuccessResponse(roleDto, "Role retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<RoleDto>.FailureResponse(
                "Error retrieving role",
                new List<string> { ex.Message }
            );
        }
    }
}
