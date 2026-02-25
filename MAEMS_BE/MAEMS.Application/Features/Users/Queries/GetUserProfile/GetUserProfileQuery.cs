using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Users.Queries.GetUserProfile;

public record GetUserProfileQuery(int UserId) : IRequest<BaseResponse<UserProfileDto>>;
