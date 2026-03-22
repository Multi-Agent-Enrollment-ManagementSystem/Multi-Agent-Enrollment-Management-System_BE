using AutoMapper;
using MAEMS.Application.DTOs.Notification;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, BaseResponse<List<NotificationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = (await _unitOfWork.Notifications
                .FindAsync(n => n.RecipientUserId == request.UserId))
            ?.ToList() ?? [];

        var dtos = notifications
            .OrderByDescending(n => n.SentAt ?? DateTime.MinValue)
            .ThenByDescending(n => n.NotificationId)
            .Select(n => _mapper.Map<NotificationDto>(n))
            .ToList();

        return BaseResponse<List<NotificationDto>>.SuccessResponse(dtos, "Notifications retrieved successfully");
    }
}
