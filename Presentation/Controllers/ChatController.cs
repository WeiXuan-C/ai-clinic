using AiClinic.Application.Commands;
using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Presentation.Controllers;

// Adapter Pattern - Adapts chat services to presentation layer
public class ChatController
{
    private readonly ICommandHandler<CreateConversationCommand, ConversationDto> _createConversationHandler;
    private readonly IChatService _chatService;

    public ChatController(
        ICommandHandler<CreateConversationCommand, ConversationDto> createConversationHandler,
        IChatService chatService)
    {
        _createConversationHandler = createConversationHandler;
        _chatService = chatService;
    }

    public async Task<ConversationDto> StartConversationAsync(Guid patientId)
    {
        var command = new CreateConversationCommand { PatientId = patientId };
        return await _createConversationHandler.HandleAsync(command);
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
    {
        return await _chatService.SendMessageAsync(request);
    }

    public async Task<ConversationDto?> GetConversationAsync(Guid conversationId)
    {
        return await _chatService.GetConversationAsync(conversationId);
    }

    public async Task<IEnumerable<ConversationDto>> GetPatientConversationsAsync(Guid patientId)
    {
        return await _chatService.GetPatientConversationsAsync(patientId);
    }
}
