using ai_clinic.Interfaces;

namespace ai_clinic.Controller;

public class MessageController(Services.MessageService messageService)
{
    public Task<IMessage?> SendMessageAsync(SendMessageRequest request)
    {
        return messageService.SendMessageAsync(request);
    }

    public Task<IMessage?> GetMessageByIdAsync(string messageId)
    {
        return messageService.GetMessageByIdAsync(messageId);
    }

    public Task<IEnumerable<IMessage>> GetMessagesByConversationIdAsync(string conversationId)
    {
        return messageService.GetMessagesByConversationIdAsync(conversationId);
    }

    public Task MarkMessageAsReadAsync(string messageId)
    {
        return messageService.MarkAsReadAsync(messageId);
    }

    public Task<bool> DeleteMessageAsync(string messageId)
    {
        return messageService.DeleteMessageAsync(messageId);
    }
}

public record SendMessageRequest(string ConversationId, string SenderId, string Content, string? AttachmentUrl);
