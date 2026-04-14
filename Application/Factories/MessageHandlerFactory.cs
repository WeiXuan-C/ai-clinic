using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Interfaces;

namespace AiClinic.Application.Factories;

// Abstract Factory Implementation
public class MessageHandlerFactory : IMessageHandlerFactory
{
    private readonly IAiService _aiService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public MessageHandlerFactory(
        IAiService aiService,
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _aiService = aiService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public IMessageHandler CreateHandler(string messageType)
    {
        return messageType.ToLower() switch
        {
            "ai" => new AiMessageHandler(_aiService, _messageRepository, _conversationRepository),
            "doctor" => new DoctorMessageHandler(_messageRepository, _conversationRepository),
            "patient" => new PatientMessageHandler(_messageRepository, _conversationRepository),
            _ => throw new ArgumentException($"Unknown message type: {messageType}")
        };
    }
}

// Concrete Handlers
public class AiMessageHandler : IMessageHandler
{
    private readonly IAiService _aiService;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public AiMessageHandler(
        IAiService aiService,
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _aiService = aiService;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<MessageDto> HandleAsync(SendMessageRequest request)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation == null)
            throw new InvalidOperationException("Conversation not found");

        var aiResponse = await _aiService.GenerateResponseAsync(request.Content, conversation.PatientId);
        
        var message = new Core.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            Content = aiResponse,
            SenderType = Core.Entities.MessageSenderType.AI,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.AddAsync(message);

        return new MessageDto(
            message.Id,
            message.Content,
            message.SenderType.ToString(),
            message.SentAt,
            message.IsRead
        );
    }
}

public class DoctorMessageHandler : IMessageHandler
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public DoctorMessageHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<MessageDto> HandleAsync(SendMessageRequest request)
    {
        var message = new Core.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            SenderType = Core.Entities.MessageSenderType.Doctor,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.AddAsync(message);

        return new MessageDto(
            message.Id,
            message.Content,
            message.SenderType.ToString(),
            message.SentAt,
            message.IsRead
        );
    }
}

public class PatientMessageHandler : IMessageHandler
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;

    public PatientMessageHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<MessageDto> HandleAsync(SendMessageRequest request)
    {
        var message = new Core.Entities.Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = request.SenderId,
            Content = request.Content,
            SenderType = Core.Entities.MessageSenderType.Patient,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.AddAsync(message);

        return new MessageDto(
            message.Id,
            message.Content,
            message.SenderType.ToString(),
            message.SentAt,
            message.IsRead
        );
    }
}
