using MAEMS.Application.DTOs.Notification;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using AutoMapper;

namespace MAEMS.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubService _notificationHubService;
    private readonly IMapper _mapper;

    public MarkNotificationReadCommandHandler(
        IUnitOfWork unitOfWork, 
        INotificationHubService notificationHubService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationHubService = notificationHubService;
        _mapper = mapper;
    }

    public async Task<BaseResponse<bool>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null)
            {
                return BaseResponse<bool>.FailureResponse(
                    "Notification not found",
                    new List<string> { $"Notification with ID {request.NotificationId} does not exist" });
            }

            if (!notification.RecipientUserId.HasValue || notification.RecipientUserId.Value != request.UserId)
            {
                return BaseResponse<bool>.FailureResponse(
                    "Forbidden",
                    new List<string> { "You are not authorized to modify this notification" });
            }

            notification.IsRead = true;

            await _unitOfWork.Notifications.UpdateAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // Send real-time notification update via SignalR
            var notificationDto = _mapper.Map<NotificationDto>(notification);
            await _notificationHubService.SendToUserAsync(request.UserId, notificationDto);

            return BaseResponse<bool>.SuccessResponse(true, "Notification marked as read");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.FailureResponse(
                "Error marking notification as read",
                new List<string> { ex.Message });
        }
    }
}
