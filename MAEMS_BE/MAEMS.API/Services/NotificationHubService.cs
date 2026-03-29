using MAEMS.Application.DTOs.Notification;
using MAEMS.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MAEMS.API.Services;

/// <summary>
/// Implementation of INotificationHubService
/// Service to interact with NotificationHub from application layer (Commands/Handlers)
/// Provides methods to send notifications to users via SignalR
/// </summary>
public class NotificationHubService : INotificationHubService
{
    private readonly IHubContext<Hubs.NotificationHub> _hubContext;
    private readonly ILogger<NotificationHubService> _logger;

    public NotificationHubService(
        IHubContext<Hubs.NotificationHub> hubContext,
        ILogger<NotificationHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Send notification to specific user via their group
    /// </summary>
    public async Task SendToUserAsync(int userId, NotificationDto notification)
    {
        try
        {
            // Send to the user's group (named "user-{userId}")
            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation($"Notification sent to user {userId}: {notification.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending notification to user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Broadcast notification to multiple users
    /// </summary>
    public async Task BroadcastToUsersAsync(IEnumerable<int> userIds, NotificationDto notification)
    {
        try
        {
            var tasks = userIds.Select(userId => SendToUserAsync(userId, notification));
            await Task.WhenAll(tasks);

            _logger.LogInformation($"Notification broadcasted to {userIds.Count()} users");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting notification: {ex.Message}");
            throw;
        }
    }
}

