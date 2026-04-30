using ai_clinic.Interfaces;
using ai_clinic.UI.State;

namespace ai_clinic.Services;

/// <summary>
/// Message Service - Business Logic Layer
/// Handles message operations through state management
/// </summary>
public class MessageService
{
    private readonly MessageState _state;

    public MessageService(MessageState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets all messages
    /// </summary>
    public async Task<IEnumerable<IMessage>> GetAllMessagesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a message by ID
    /// </summary>
    public async Task<IMessage?> GetMessageByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets messages by conversation ID
    /// </summary>
    public async Task<IEnumerable<IMessage>> GetConversationMessagesAsync(Guid conversationId)
    {
        return await _state.GetByConversationIdAsync(conversationId);
    }

    /// <summary>
    /// Gets the latest message in a conversation
    /// </summary>
    public async Task<IMessage?> GetLatestMessageAsync(Guid conversationId)
    {
        return await _state.GetLatestMessageAsync(conversationId);
    }

    /// <summary>
    /// Gets unread message count for a conversation
    /// </summary>
    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        return await _state.GetUnreadCountAsync(conversationId, userId);
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageAsReadAsync(Guid messageId)
    {
        await _state.MarkAsReadAsync(messageId);
    }

    /// <summary>
    /// Creates a new message
    /// </summary>
    public async Task<IMessage?> CreateMessageAsync(IMessage message)
    {
        var concreteMessage = message as Message ?? new Message
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderType = message.SenderType,
            Content = message.Content,
            AiModelUsed = message.AiModelUsed,
            AiConfidenceScore = message.AiConfidenceScore,
            DocumentReferences = message.DocumentReferences,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt,
            CreatedAt = message.CreatedAt
        };
        return await _state.CreateAsync(concreteMessage);
    }

    /// <summary>
    /// Updates a message
    /// </summary>
    public async Task<IMessage?> UpdateMessageAsync(IMessage message)
    {
        var concreteMessage = message as Message ?? new Message
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderType = message.SenderType,
            Content = message.Content,
            AiModelUsed = message.AiModelUsed,
            AiConfidenceScore = message.AiConfidenceScore,
            DocumentReferences = message.DocumentReferences,
            IsRead = message.IsRead,
            ReadAt = message.ReadAt,
            CreatedAt = message.CreatedAt
        };
        return await _state.UpdateAsync(concreteMessage);
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    public async Task<bool> DeleteMessageAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets cached messages from state
    /// </summary>
    public IReadOnlyList<IMessage> GetCachedMessages()
    {
        return _state.Messages.Cast<IMessage>().ToList();
    }

    /// <summary>
    /// Gets the currently selected message
    /// </summary>
    public IMessage? GetSelectedMessage()
    {
        return _state.SelectedMessage;
    }

    /// <summary>
    /// Sets the selected message
    /// </summary>
    public void SetSelectedMessage(IMessage? message)
    {
        _state.SelectedMessage = message as Message;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IMessage?> SendMessageAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var conversationId = Guid.Parse(requestType.GetProperty("ConversationId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var senderId = requestType.GetProperty("SenderId")?.GetValue(request)?.ToString();
        var content = requestType.GetProperty("Content")?.GetValue(request)?.ToString() ?? string.Empty;
        var senderType = requestType.GetProperty("SenderType")?.GetValue(request)?.ToString() ?? "user";
        
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId != null ? Guid.Parse(senderId) : null,
            SenderType = senderType,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        
        return await CreateMessageAsync((IMessage)message);
    }
    
    public async Task<IMessage?> GetMessageByIdAsync(string messageId)
    {
        if (Guid.TryParse(messageId, out var guid))
        {
            return await GetMessageByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<IEnumerable<IMessage>> GetMessagesByConversationIdAsync(string conversationId)
    {
        if (Guid.TryParse(conversationId, out var guid))
        {
            return await GetConversationMessagesAsync(guid);
        }
        return Enumerable.Empty<IMessage>();
    }
    
    public async Task MarkAsReadAsync(string messageId)
    {
        if (Guid.TryParse(messageId, out var guid))
        {
            await MarkMessageAsReadAsync(guid);
        }
    }
    
    public async Task<bool> DeleteMessageAsync(string messageId)
    {
        if (Guid.TryParse(messageId, out var guid))
        {
            return await DeleteMessageAsync(guid);
        }
        return false;
    }
}
