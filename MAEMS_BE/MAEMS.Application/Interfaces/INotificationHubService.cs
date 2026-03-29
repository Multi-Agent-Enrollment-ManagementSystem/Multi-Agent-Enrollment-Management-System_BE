using MAEMS.Application.DTOs.Notification;

namespace MAEMS.Application.Interfaces;

/// <summary>
/// Interface for SignalR NotificationHub service
/// Used by application layer (Command/Query handlers) to send real-time notifications
/// </summary>
public interface INotificationHubService
{
    /// <summary>
    /// Send a notification to a specific user via SignalR
    /// </summary>
    /// <param name="userId">The recipient user ID</param>
    /// <param name="notification">The notification DTO to send</param>
    Task SendToUserAsync(int userId, NotificationDto notification);

    /// <summary>
    /// Broadcast notification to multiple users via SignalR
    /// </summary>
    /// <param name="userIds">List of recipient user IDs</param>
    /// <param name="notification">The notification DTO to send</param>
    Task BroadcastToUsersAsync(IEnumerable<int> userIds, NotificationDto notification);
}
