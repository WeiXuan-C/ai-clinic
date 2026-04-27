using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Services;

/// <summary>
/// Chat Service - Business Logic Layer
/// Handles message routing, AI integration, and conversation management
/// </summary>
public class ChatService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IDocumentRepository _documentRepository;

    public ChatService(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IDocumentRepository documentRepository)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _documentRepository = documentRepository;
    }

    /// <summary>
    /// Creates a new conversation for a patient
    /// </summary>
    public async Task<Conversation> CreateConversationAsync(Guid patientId, string? title = null)
    {
        var conversation = Conversation.Create(patientId, title);
        return await _conversationRepository.AddAsync(conversation);
    }

    /// <summary>
    /// Sends a message to a doctor
    /// </summary>
    public async Task<Message> SendToDoctorAsync(Guid conversationId, Guid senderId, string content)
    {
        var message = Message.CreatePatientMessage(conversationId, senderId, content);
        var savedMessage = await _messageRepository.AddAsync(message);
        
        // Update conversation stats
        await UpdateConversationStatsAsync(conversationId);
        
        return savedMessage;
    }

    /// <summary>
    /// Sends a message to AI and generates response
    /// </summary>
    public async Task<Message> SendToAIAsync(Guid conversationId, Guid senderId, string content)
    {
        // Save patient message
        var patientMessage = Message.CreatePatientMessage(conversationId, senderId, content);
        await _messageRepository.AddAsync(patientMessage);
        
        // Get relevant documents for context
        var documents = await _documentRepository.GetProcessedDocumentsAsync(conversationId);
        
        // Generate AI response (placeholder - integrate with actual AI service)
        var aiResponse = await GenerateAIResponseAsync(content, documents);
        
        // Save AI message
        var aiMessage = Message.CreateAIMessage(conversationId, aiResponse, "gpt-4", 0.85m);
        var savedAiMessage = await _messageRepository.AddAsync(aiMessage);
        
        // Update conversation stats
        await UpdateConversationStatsAsync(conversationId, isAiMessage: true);
        
        return savedAiMessage;
    }

    /// <summary>
    /// Assigns a doctor to a conversation
    /// </summary>
    public async Task<Conversation> AssignDoctorAsync(Guid conversationId, Guid doctorId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        
        if (conversation == null)
        {
            throw new Exception("Conversation not found");
        }
        
        var updatedConversation = conversation.WithAssignedDoctor(doctorId);
        return await _conversationRepository.UpdateAsync(updatedConversation);
    }

    /// <summary>
    /// Gets all messages in a conversation
    /// </summary>
    public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId)
    {
        return await _messageRepository.GetByConversationIdAsync(conversationId);
    }

    /// <summary>
    /// Gets active conversation for a patient
    /// </summary>
    public async Task<Conversation?> GetActiveConversationAsync(Guid patientId)
    {
        return await _conversationRepository.GetActiveConversationByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Gets all conversations for a patient
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetPatientConversationsAsync(Guid patientId)
    {
        return await _conversationRepository.GetByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageAsReadAsync(Guid messageId)
    {
        await _messageRepository.MarkAsReadAsync(messageId);
    }

    /// <summary>
    /// Updates conversation statistics
    /// </summary>
    private async Task UpdateConversationStatsAsync(Guid conversationId, bool isAiMessage = false)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        
        if (conversation != null)
        {
            var senderType = isAiMessage ? MessageSenderType.AI : MessageSenderType.Doctor;
            var updatedConversation = conversation.WithAddedMessage(senderType);
            await _conversationRepository.UpdateAsync(updatedConversation);
        }
    }

    /// <summary>
    /// Generates AI response based on content and documents (placeholder)
    /// TODO: Integrate with actual AI service (OpenAI, Anthropic, etc.)
    /// </summary>
    private async Task<string> GenerateAIResponseAsync(string content, IEnumerable<Document> documents)
    {
        // Placeholder AI response
        await Task.Delay(100); // Simulate API call
        
        var documentContext = documents.Any() 
            ? $"\n\nBased on your uploaded documents, " 
            : "\n\n";
        
        return $"I understand you're asking about: {content}.{documentContext}I recommend consulting with a healthcare professional for personalized advice. Would you like me to connect you with an available doctor?";
    }
}
