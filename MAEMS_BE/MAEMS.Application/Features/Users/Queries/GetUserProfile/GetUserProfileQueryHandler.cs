using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, BaseResponse<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserProfileQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

            if (user == null)
            {
                return BaseResponse<UserProfileDto>.FailureResponse(
                    $"User with ID {request.UserId} not found",
                    new List<string> { "User not found" }
                );
            }

            // Check if user is active
            if (user.IsActive == false)
            {
                return BaseResponse<UserProfileDto>.FailureResponse(
                    "User account is inactive",
                    new List<string> { "User account is disabled" }
                );
            }

            // Map user to UserProfileDto
            var userProfileDto = _mapper.Map<UserProfileDto>(user);

            // Get role name if roleId exists
            if (user.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value);
                userProfileDto.RoleName = role?.Name;
            }

            return BaseResponse<UserProfileDto>.SuccessResponse(userProfileDto, "User profile retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<UserProfileDto>.FailureResponse(
                "Error retrieving user profile",
                new List<string> { ex.Message }
            );
        }
    }
}
