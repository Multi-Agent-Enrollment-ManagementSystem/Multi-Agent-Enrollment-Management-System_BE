using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MAEMS.API.Hubs;

/// <summary>
/// SignalR Hub for real-time notifications
/// Handles WebSocket connections and sends notifications to connected users
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private static readonly Dictionary<int, string> UserConnections = new();
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// Extracts UserId from JWT token and adds to group for targeted messaging
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserIdFromClaims();
        _logger.LogInformation($"🔌 NotificationHub Connection Attempt - UserId: {userId}, ConnectionId: {Context.ConnectionId}");

        if (userId > 0)
        {
            // Store the connection ID for this user
            UserConnections[userId] = Context.ConnectionId;

            // Add user to a group named after their ID for targeted messaging
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            _logger.LogInformation($"✅ User {userId} connected successfully. ConnectionId: {Context.ConnectionId}, Group: user-{userId}");

            await base.OnConnectedAsync();
        }
        else
        {
            _logger.LogWarning($"❌ Connection rejected - No valid UserId found. ConnectionId: {Context.ConnectionId}");
            // Disconnect if no valid user ID
            Context.Abort();
        }
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// Cleans up connection mapping and group membership
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserIdFromClaims();

        if (userId > 0 && UserConnections.ContainsKey(userId))
        {
            UserConnections.Remove(userId);
            _logger.LogInformation($"👋 User {userId} disconnected. Exception: {exception?.Message}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Extracts UserId from JWT claims
    /// </summary>
    /// <returns>UserId if found, otherwise 0</returns>
    private int GetUserIdFromClaims()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return 0;
        }

        return userId;
    }

    /// <summary>
    /// Get all connected users (for debugging/monitoring)
    /// </summary>
    public IEnumerable<int> GetConnectedUsers()
    {
        return UserConnections.Keys;
    }
}
