using AiClinic.Application.DTOs;
using AiClinic.Application.Factories;
using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageHandlerFactory _messageHandlerFactory;
    private readonly IDoctorAssignmentService _doctorAssignmentService;

    public ChatService(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMessageHandlerFactory messageHandlerFactory,
        IDoctorAssignmentService doctorAssignmentService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _messageHandlerFactory = messageHandlerFactory;
        _doctorAssignmentService = doctorAssignmentService;
    }

    public async Task<ConversationDto> CreateConversationAsync(Guid patientId)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Status = ConversationStatus.Active,
            Type = ConversationType.AI,
            CreatedAt = DateTime.UtcNow
        };

        conversation = await _conversationRepository.AddAsync(conversation);

        return new ConversationDto(
            conversation.Id,
            conversation.PatientId,
            conversation.DoctorId,
            conversation.Status.ToString(),
            conversation.Type.ToString(),
            conversation.CreatedAt,
            new List<MessageDto>()
        );
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
    {
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation == null)
            throw new InvalidOperationException("Conversation not found");

        // Chat routing logic
        if (conversation.DoctorId.HasValue)
        {
            // Route to doctor
            var handler = _messageHandlerFactory.CreateHandler("doctor");
            return await handler.HandleAsync(request);
        }
        else
        {
            // Try to find available doctor
            var doctor = await _doctorAssignmentService.FindBestAvailableDoctorAsync();
            
            if (doctor != null)
            {
                // Assign doctor and route
                await _doctorAssignmentService.AssignDoctorToConversationAsync(conversation.Id, doctor.Id);
                conversation.Type = ConversationType.Doctor;
                await _conversationRepository.UpdateAsync(conversation);
                
                var handler = _messageHandlerFactory.CreateHandler("doctor");
                return await handler.HandleAsync(request);
            }
            else
            {
                // Route to AI
                var handler = _messageHandlerFactory.CreateHandler("ai");
                return await handler.HandleAsync(request);
            }
        }
    }

    public async Task<ConversationDto?> GetConversationAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            return null;

        var messages = await _messageRepository.GetByConversationIdAsync(conversationId);
        var messageDtos = messages.Select(m => new MessageDto(
            m.Id,
            m.Content,
            m.SenderType.ToString(),
            m.SentAt,
            m.IsRead
        )).ToList();

        return new ConversationDto(
            conversation.Id,
            conversation.PatientId,
            conversation.DoctorId,
            conversation.Status.ToString(),
            conversation.Type.ToString(),
            conversation.CreatedAt,
            messageDtos
        );
    }

    public async Task<IEnumerable<ConversationDto>> GetPatientConversationsAsync(Guid patientId)
    {
        var conversations = await _conversationRepository.GetByPatientIdAsync(patientId);
        var result = new List<ConversationDto>();

        foreach (var conversation in conversations)
        {
            var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id);
            var messageDtos = messages.Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.SenderType.ToString(),
                m.SentAt,
                m.IsRead
            )).ToList();

            result.Add(new ConversationDto(
                conversation.Id,
                conversation.PatientId,
                conversation.DoctorId,
                conversation.Status.ToString(),
                conversation.Type.ToString(),
                conversation.CreatedAt,
                messageDtos
            ));
        }

        return result;
    }
}
