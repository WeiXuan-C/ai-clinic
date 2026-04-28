namespace AiClinic.Controller;

public class MessageController
{
    private readonly Interfaces.MessageInterface _messageService;

    public MessageController(Interfaces.MessageInterface messageService)
    {
        _messageService = messageService;
    }

    public async Task<object> SendMessageAsync(SendMessageRequest request)
    {
        return await _messageService.SendMessageAsync(request);
    }

    public async Task<object?> GetMessageByIdAsync(string messageId)
    {
        return await _messageService.GetMessageByIdAsync(messageId);
    }

    public async Task<object> GetMessagesByConversationIdAsync(string conversationId, int limit = 50, int offset = 0)
    {
        return await _messageService.GetMessagesByConversationIdAsync(conversationId, limit, offset);
    }

    public async Task MarkMessageAsReadAsync(string messageId)
    {
        await _messageService.MarkMessageAsReadAsync(messageId);
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        await _messageService.DeleteMessageAsync(messageId);
    }
}

public record SendMessageRequest(string ConversationId, string SenderId, string Content, string? AttachmentUrl);
