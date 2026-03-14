using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetUserDetailById;

public class GetUserDetailByIdQueryHandler : IRequestHandler<GetUserDetailByIdQuery, BaseResponse<UserDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetUserDetailByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<UserDetailDto>> Handle(GetUserDetailByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return BaseResponse<UserDetailDto>.FailureResponse(
                    $"User with ID {request.UserId} not found",
                    new List<string> { "User not found" });
            }

            var dto = _mapper.Map<UserDetailDto>(user);

            if (user.RoleId.HasValue)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value);
                dto.RoleName = role?.Name;
            }

            // If roleId == 4 (applicant) then include applicant profile
            if (user.RoleId == 4)
            {
                var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(user.UserId);
                dto.Applicant = applicant != null
                    ? _mapper.Map<ApplicantDto>(applicant)
                    : null;
            }

            return BaseResponse<UserDetailDto>.SuccessResponse(dto, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<UserDetailDto>.FailureResponse(
                "Error retrieving user",
                new List<string> { ex.Message });
        }
    }
}
