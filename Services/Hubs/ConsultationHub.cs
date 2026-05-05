using Microsoft.AspNetCore.SignalR;
using ai_clinic.Models;
using System.Collections.Concurrent;

namespace ai_clinic.Services.Hubs;

/// <summary>
/// SignalR Hub for real-time consultation messaging
/// Handles real-time communication between patients, doctors, and AI
/// 
/// Features:
/// - Real-time message delivery
/// - Typing indicators
/// - Online status tracking
/// - Group-based conversation isolation
/// </summary>
public class ConsultationHub : Hub
{
    // Track online users (ConnectionId -> UserId mapping)
    private static readonly ConcurrentDictionary<string, Guid> _onlineUsers = new();
    
    // Track user connections (UserId -> List of ConnectionIds)
    private static readonly ConcurrentDictionary<Guid, HashSet<string>> _userConnections = new();

    private readonly ILogger<ConsultationHub> _logger;

    public ConsultationHub(ILogger<ConsultationHub> logger)
    {
        _logger = logger;
    }

    #region Connection Management

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        // Remove from online users
        if (_onlineUsers.TryRemove(connectionId, out var userId))
        {
            // Remove this connection from user's connection list
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                
                // If user has no more connections, remove from tracking
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                    _logger.LogInformation($"User {userId} is now offline");
                }
            }
        }

        _logger.LogInformation($"Client disconnected: {connectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Register user connection with their UserId
    /// </summary>
    public async Task RegisterUser(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            _logger.LogWarning($"Invalid userId format: {userId}");
            return;
        }

        var connectionId = Context.ConnectionId;
        
        // Add to online users
        _onlineUsers[connectionId] = userGuid;
        
        // Add to user connections
        _userConnections.AddOrUpdate(
            userGuid,
            new HashSet<string> { connectionId },
            (key, existing) =>
            {
                existing.Add(connectionId);
                return existing;
            }
        );

        _logger.LogInformation($"User {userGuid} registered with connection {connectionId}");
        await Task.CompletedTask;
    }

    #endregion

    #region Conversation Group Management

    /// <summary>
    /// Join a conversation group to receive messages for that conversation
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var conversationGuid))
        {
            _logger.LogWarning($"Invalid conversationId format: {conversationId}");
            return;
        }

        var groupName = GetConversationGroupName(conversationGuid);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation($"Connection {Context.ConnectionId} joined conversation {conversationId}");
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var conversationGuid))
        {
            _logger.LogWarning($"Invalid conversationId format: {conversationId}");
            return;
        }

        var groupName = GetConversationGroupName(conversationGuid);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation($"Connection {Context.ConnectionId} left conversation {conversationId}");
    }

    #endregion

    #region Typing Indicators

    /// <summary>
    /// Notify other participants that user is typing
    /// </summary>
    public async Task NotifyTyping(string conversationId, string userName, string userRole)
    {
        if (!Guid.TryParse(conversationId, out var conversationGuid))
        {
            _logger.LogWarning($"Invalid conversationId format: {conversationId}");
            return;
        }

        var groupName = GetConversationGroupName(conversationGuid);
        
        // Send to all in group except sender
        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            ConversationId = conversationId,
            UserName = userName,
            UserRole = userRole,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify other participants that user stopped typing
    /// </summary>
    public async Task NotifyStoppedTyping(string conversationId, string userName)
    {
        if (!Guid.TryParse(conversationId, out var conversationGuid))
        {
            _logger.LogWarning($"Invalid conversationId format: {conversationId}");
            return;
        }

        var groupName = GetConversationGroupName(conversationGuid);
        
        await Clients.OthersInGroup(groupName).SendAsync("UserStoppedTyping", new
        {
            ConversationId = conversationId,
            UserName = userName,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get standardized group name for a conversation
    /// </summary>
    private static string GetConversationGroupName(Guid conversationId)
    {
        return $"conversation_{conversationId}";
    }

    /// <summary>
    /// Check if a user is currently online
    /// </summary>
    public static bool IsUserOnline(Guid userId)
    {
        return _userConnections.ContainsKey(userId);
    }

    /// <summary>
    /// Get all connection IDs for a specific user
    /// </summary>
    public static IEnumerable<string> GetUserConnections(Guid userId)
    {
        if (_userConnections.TryGetValue(userId, out var connections))
        {
            return connections.ToList();
        }
        return Enumerable.Empty<string>();
    }

    #endregion
}

/// <summary>
/// Extension methods for IHubContext to simplify sending messages
/// </summary>
public static class ConsultationHubExtensions
{
    /// <summary>
    /// Send a new message notification to all participants in a conversation
    /// </summary>
    public static async Task SendMessageToConversation(
        this IHubContext<ConsultationHub> hubContext,
        Guid conversationId,
        Message message)
    {
        var groupName = $"conversation_{conversationId}";
        
        await hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", new
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderType = message.SenderType.ToString(),
            Content = message.Content,
            AiModelUsed = message.AiModelUsed,
            AiConfidenceScore = message.AiConfidenceScore,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        });
    }

    /// <summary>
    /// Send AI response status update (e.g., "AI is thinking...")
    /// </summary>
    public static async Task SendAiStatusUpdate(
        this IHubContext<ConsultationHub> hubContext,
        Guid conversationId,
        string status,
        string? details = null)
    {
        var groupName = $"conversation_{conversationId}";
        
        await hubContext.Clients.Group(groupName).SendAsync("AiStatusUpdate", new
        {
            ConversationId = conversationId,
            Status = status,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Notify conversation participants about status changes
    /// </summary>
    public static async Task SendConversationStatusUpdate(
        this IHubContext<ConsultationHub> hubContext,
        Guid conversationId,
        ConversationStatus status)
    {
        var groupName = $"conversation_{conversationId}";
        
        await hubContext.Clients.Group(groupName).SendAsync("ConversationStatusChanged", new
        {
            ConversationId = conversationId,
            Status = status.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send message read receipt
    /// </summary>
    public static async Task SendMessageReadReceipt(
        this IHubContext<ConsultationHub> hubContext,
        Guid conversationId,
        Guid messageId,
        Guid readByUserId)
    {
        var groupName = $"conversation_{conversationId}";
        
        await hubContext.Clients.Group(groupName).SendAsync("MessageRead", new
        {
            ConversationId = conversationId,
            MessageId = messageId,
            ReadByUserId = readByUserId,
            Timestamp = DateTime.UtcNow
        });
    }
}
