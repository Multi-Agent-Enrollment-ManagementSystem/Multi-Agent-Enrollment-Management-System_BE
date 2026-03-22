namespace MAEMS.Domain.Entities;

public sealed class Notification
{
    public int NotificationId { get; set; }

    public int? RecipientUserId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool? IsRead { get; set; }

    public DateTime? SentAt { get; set; }
}
