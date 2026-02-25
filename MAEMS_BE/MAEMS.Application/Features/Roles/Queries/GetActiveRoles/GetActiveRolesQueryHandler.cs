using AutoMapper;
using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Roles.Queries.GetActiveRoles;

public class GetActiveRolesQueryHandler : IRequestHandler<GetActiveRolesQuery, BaseResponse<IEnumerable<RoleDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveRolesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<RoleDto>>> Handle(GetActiveRolesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _unitOfWork.Roles.GetActiveRolesAsync();
            var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(roles);

            return BaseResponse<IEnumerable<RoleDto>>.SuccessResponse(roleDtos, "Active roles retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<RoleDto>>.FailureResponse(
                "Error retrieving active roles",
                new List<string> { ex.Message }
            );
        }
    }
}
