using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IChatService
{
    Task<ConversationDto> CreateConversationAsync(Guid patientId);
    Task<MessageDto> SendMessageAsync(SendMessageRequest request);
    Task<ConversationDto?> GetConversationAsync(Guid conversationId);
    Task<IEnumerable<ConversationDto>> GetPatientConversationsAsync(Guid patientId);
}
