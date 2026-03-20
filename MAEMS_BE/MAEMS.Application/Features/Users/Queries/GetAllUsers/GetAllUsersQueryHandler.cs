using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, BaseResponse<PagedResponse<UserDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (users, totalCount) = await _unitOfWork.Users.GetUsersPagedAsync(
                request.RoleId,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Preload roles to avoid N+1 queries
            var roles = (await _unitOfWork.Roles.GetAllAsync())
                .ToDictionary(r => r.RoleId, r => r.Name);

            var dtos = _mapper.Map<List<UserDto>>(users);

            foreach (var dto in dtos)
            {
                if (dto.RoleId.HasValue && roles.TryGetValue(dto.RoleId.Value, out var roleName))
                {
                    dto.RoleName = roleName;
                }
            }

            var paged = new PagedResponse<UserDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<UserDto>>.SuccessResponse(
                paged,
                $"Users retrieved successfully. Found {totalCount} user(s)."
            );
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<UserDto>>.FailureResponse(
                "Error retrieving users",
                new List<string> { ex.Message }
            );
        }
    }
}
