using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommand : IRequest<BaseResponse<bool>>
{
    public int NotificationId { get; set; }

    [JsonIgnore]
    public int UserId { get; set; }
}
