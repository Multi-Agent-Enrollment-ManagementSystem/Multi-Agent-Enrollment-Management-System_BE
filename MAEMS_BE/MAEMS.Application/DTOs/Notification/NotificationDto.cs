namespace MAEMS.Application.DTOs.Notification;

public sealed class NotificationDto
{
    public int NotificationId { get; set; }
    public string? NotificationType { get; set; }
    public string? Message { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? SentAt { get; set; }
}
