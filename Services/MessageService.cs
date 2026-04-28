using AiClinic.UI.State;

namespace AiClinic.Services;

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
    public async Task<IEnumerable<Message>> GetAllMessagesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a message by ID
    /// </summary>
    public async Task<Message?> GetMessageByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets messages by conversation ID
    /// </summary>
    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId)
    {
        return await _state.GetByConversationIdAsync(conversationId);
    }

    /// <summary>
    /// Gets the latest message in a conversation
    /// </summary>
    public async Task<Message?> GetLatestMessageAsync(Guid conversationId)
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
    public async Task<Message?> CreateMessageAsync(Message message)
    {
        return await _state.CreateAsync(message);
    }

    /// <summary>
    /// Updates a message
    /// </summary>
    public async Task<Message?> UpdateMessageAsync(Message message)
    {
        return await _state.UpdateAsync(message);
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
    public IReadOnlyList<Message> GetCachedMessages()
    {
        return _state.Messages;
    }

    /// <summary>
    /// Gets the currently selected message
    /// </summary>
    public Message? GetSelectedMessage()
    {
        return _state.SelectedMessage;
    }

    /// <summary>
    /// Sets the selected message
    /// </summary>
    public void SetSelectedMessage(Message? message)
    {
        _state.SelectedMessage = message;
    }
}
