using AutoMapper;
using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, BaseResponse<IEnumerable<RoleDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllRolesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();
            var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);

            return BaseResponse<IEnumerable<RoleDto>>.SuccessResponse(roleDtos, "Roles retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<RoleDto>>.FailureResponse(
                "Error retrieving roles", 
                new List<string> { ex.Message }
            );
        }
    }
}
