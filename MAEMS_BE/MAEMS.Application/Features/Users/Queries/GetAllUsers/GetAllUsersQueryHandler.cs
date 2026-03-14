using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, BaseResponse<IEnumerable<UserDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllUsersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = (await _unitOfWork.Users.GetAllAsync()).ToList();

            if (request.RoleId.HasValue)
            {
                users = users.Where(u => u.RoleId == request.RoleId.Value).ToList();
            }

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

            return BaseResponse<IEnumerable<UserDto>>.SuccessResponse(
                dtos,
                $"Users retrieved successfully. Found {dtos.Count} user(s)."
            );
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<UserDto>>.FailureResponse(
                "Error retrieving users",
                new List<string> { ex.Message }
            );
        }
    }
}
