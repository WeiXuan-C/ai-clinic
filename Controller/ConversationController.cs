namespace AiClinic.Controller;

public class ConversationController
{
    private readonly Interfaces.ConversationInterface _conversationService;

    public ConversationController(Interfaces.ConversationInterface conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<object> CreateConversationAsync(CreateConversationRequest request)
    {
        return await _conversationService.CreateConversationAsync(request);
    }

    public async Task<object?> GetConversationByIdAsync(string conversationId)
    {
        return await _conversationService.GetConversationByIdAsync(conversationId);
    }

    public async Task<object> GetConversationsByUserIdAsync(string userId)
    {
        return await _conversationService.GetConversationsByUserIdAsync(userId);
    }

    public async Task<object?> GetConversationBetweenUsersAsync(string patientId, string doctorId)
    {
        return await _conversationService.GetConversationBetweenUsersAsync(patientId, doctorId);
    }

    public async Task<object> UpdateConversationAsync(string conversationId, UpdateConversationRequest request)
    {
        return await _conversationService.UpdateConversationAsync(conversationId, request);
    }

    public async Task DeleteConversationAsync(string conversationId)
    {
        await _conversationService.DeleteConversationAsync(conversationId);
    }
}

public record CreateConversationRequest(string PatientId, string DoctorId);
public record UpdateConversationRequest(string Status, DateTime? LastMessageAt);
