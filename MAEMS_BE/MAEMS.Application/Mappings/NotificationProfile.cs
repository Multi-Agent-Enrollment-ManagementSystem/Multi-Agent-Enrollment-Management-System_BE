using AutoMapper;
using MAEMS.Application.DTOs.Notification;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public sealed class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
