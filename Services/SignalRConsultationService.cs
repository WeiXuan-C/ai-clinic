using Microsoft.AspNetCore.SignalR.Client;
using ai_clinic.Models;

namespace ai_clinic.Services;

/// <summary>
/// Client-side SignalR service for real-time consultation messaging
/// Manages SignalR connection and provides event handlers for real-time updates
/// 
/// Design Pattern: OBSERVER PATTERN
/// - Clients subscribe to events (ReceiveMessage, UserTyping, etc.)
/// - Service notifies subscribers when events occur
/// </summary>
public class SignalRConsultationService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILogger<SignalRConsultationService> _logger;
    private bool _isConnected = false;

    // Event handlers for real-time updates
    public event Action<MessageReceivedEventArgs>? OnMessageReceived;
    public event Action<AiStatusEventArgs>? OnAiStatusUpdate;
    public event Action<TypingEventArgs>? OnUserTyping;
    public event Action<TypingEventArgs>? OnUserStoppedTyping;
    public event Action<ConversationStatusEventArgs>? OnConversationStatusChanged;
    public event Action<MessageReadEventArgs>? OnMessageRead;
    public event Action? OnConnected;
    public event Action<Exception?>? OnDisconnected;
    public event Action<Exception>? OnReconnecting;
    public event Action? OnReconnected;

    public SignalRConsultationService(ILogger<SignalRConsultationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize and start SignalR connection
    /// </summary>
    public async Task InitializeAsync(string hubUrl)
    {
        if (_hubConnection != null)
        {
            _logger.LogWarning("SignalR connection already initialized");
            return;
        }

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            // Register event handlers
            RegisterEventHandlers();

            // Connection lifecycle events
            _hubConnection.Closed += OnConnectionClosed;
            _hubConnection.Reconnecting += OnConnectionReconnecting;
            _hubConnection.Reconnected += OnConnectionReconnected;

            await _hubConnection.StartAsync();
            _isConnected = true;
            _logger.LogInformation("SignalR connection established");
            
            OnConnected?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SignalR connection");
            throw;
        }
    }

    /// <summary>
    /// Register user with their UserId
    /// </summary>
    public async Task RegisterUserAsync(Guid userId)
    {
        if (_hubConnection == null || !_isConnected)
        {
            _logger.LogWarning("Cannot register user - SignalR not connected");
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("RegisterUser", userId.ToString());
            _logger.LogInformation($"User {userId} registered with SignalR hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to register user {userId}");
        }
    }

    /// <summary>
    /// Join a conversation group to receive real-time messages
    /// </summary>
    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_hubConnection == null || !_isConnected)
        {
            _logger.LogWarning("Cannot join conversation - SignalR not connected");
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("JoinConversation", conversationId.ToString());
            _logger.LogInformation($"Joined conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to join conversation {conversationId}");
        }
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversationAsync(Guid conversationId)
    {
        if (_hubConnection == null || !_isConnected)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("LeaveConversation", conversationId.ToString());
            _logger.LogInformation($"Left conversation {conversationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to leave conversation {conversationId}");
        }
    }

    /// <summary>
    /// Notify other participants that user is typing
    /// </summary>
    public async Task NotifyTypingAsync(Guid conversationId, string userName, string userRole)
    {
        if (_hubConnection == null || !_isConnected)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("NotifyTyping", conversationId.ToString(), userName, userRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send typing notification");
        }
    }

    /// <summary>
    /// Notify other participants that user stopped typing
    /// </summary>
    public async Task NotifyStoppedTypingAsync(Guid conversationId, string userName)
    {
        if (_hubConnection == null || !_isConnected)
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("NotifyStoppedTyping", conversationId.ToString(), userName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send stopped typing notification");
        }
    }

    /// <summary>
    /// Check if SignalR is connected
    /// </summary>
    public bool IsConnected => _isConnected && _hubConnection?.State == HubConnectionState.Connected;

    #region Private Methods

    private void RegisterEventHandlers()
    {
        if (_hubConnection == null) return;

        // Receive new message
        _hubConnection.On<object>("ReceiveMessage", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                Console.WriteLine($"[SignalR Service] Raw received data: {json}");
                
                // Use JsonSerializerOptions with camelCase naming policy
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var message = System.Text.Json.JsonSerializer.Deserialize<MessageReceivedEventArgs>(json, options);
                if (message != null)
                {
                    _logger.LogInformation($"Received message: {message.MessageId}");
                    Console.WriteLine($"[SignalR Service] Parsed MessageId: {message.MessageId}");
                    Console.WriteLine($"[SignalR Service] Parsed ConversationId: {message.ConversationId}");
                    Console.WriteLine($"[SignalR Service] Parsed Content: {message.Content}");
                    OnMessageReceived?.Invoke(message);
                }
                else
                {
                    Console.WriteLine($"[SignalR Service] Failed to deserialize message");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing received message");
                Console.WriteLine($"[SignalR Service] Error: {ex.Message}");
            }
        });

        // AI status update
        _hubConnection.On<object>("AiStatusUpdate", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var status = System.Text.Json.JsonSerializer.Deserialize<AiStatusEventArgs>(json, options);
                if (status != null)
                {
                    _logger.LogInformation($"AI status: {status.Status}");
                    OnAiStatusUpdate?.Invoke(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI status update");
            }
        });

        // User typing
        _hubConnection.On<object>("UserTyping", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var typing = System.Text.Json.JsonSerializer.Deserialize<TypingEventArgs>(json, options);
                if (typing != null)
                {
                    OnUserTyping?.Invoke(typing);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing typing notification");
            }
        });

        // User stopped typing
        _hubConnection.On<object>("UserStoppedTyping", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var typing = System.Text.Json.JsonSerializer.Deserialize<TypingEventArgs>(json, options);
                if (typing != null)
                {
                    OnUserStoppedTyping?.Invoke(typing);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stopped typing notification");
            }
        });

        // Conversation status changed
        _hubConnection.On<object>("ConversationStatusChanged", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var status = System.Text.Json.JsonSerializer.Deserialize<ConversationStatusEventArgs>(json, options);
                if (status != null)
                {
                    _logger.LogInformation($"Conversation status changed: {status.Status}");
                    OnConversationStatusChanged?.Invoke(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing conversation status change");
            }
        });

        // Message read receipt
        _hubConnection.On<object>("MessageRead", (data) =>
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var receipt = System.Text.Json.JsonSerializer.Deserialize<MessageReadEventArgs>(json, options);
                if (receipt != null)
                {
                    OnMessageRead?.Invoke(receipt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message read receipt");
            }
        });
    }

    private Task OnConnectionClosed(Exception? exception)
    {
        _isConnected = false;
        _logger.LogWarning(exception, "SignalR connection closed");
        OnDisconnected?.Invoke(exception);
        return Task.CompletedTask;
    }

    private Task OnConnectionReconnecting(Exception? exception)
    {
        _isConnected = false;
        _logger.LogWarning(exception, "SignalR reconnecting...");
        if (exception != null)
        {
            OnReconnecting?.Invoke(exception);
        }
        return Task.CompletedTask;
    }

    private Task OnConnectionReconnected(string? connectionId)
    {
        _isConnected = true;
        _logger.LogInformation($"SignalR reconnected with connection ID: {connectionId}");
        OnReconnected?.Invoke();
        return Task.CompletedTask;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}

#region Event Args Classes

public class MessageReceivedEventArgs
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AiModelUsed { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AiStatusEventArgs
{
    public Guid ConversationId { get; set; }
    public string Status { get; set; } = string.Empty; // "thinking", "ready", "error"
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}

public class TypingEventArgs
{
    public Guid ConversationId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserRole { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ConversationStatusEventArgs
{
    public Guid ConversationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class MessageReadEventArgs
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public Guid ReadByUserId { get; set; }
    public DateTime Timestamp { get; set; }
}

#endregion
