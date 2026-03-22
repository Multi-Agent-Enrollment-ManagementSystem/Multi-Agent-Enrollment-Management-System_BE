using MAEMS.Application.DTOs.Notification;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQuery : IRequest<BaseResponse<List<NotificationDto>>>
{
    public int UserId { get; set; }
}
