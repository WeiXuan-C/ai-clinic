using ai_clinic.Interfaces;

namespace ai_clinic.Controller;

public class ConversationController(Services.ConversationService conversationService)
{
    public Task<IConversation?> CreateConversationAsync(CreateConversationRequest request)
    {
        return conversationService.CreateConversationAsync(request);
    }

    public Task<IConversation?> GetConversationByIdAsync(string conversationId)
    {
        return conversationService.GetConversationByIdAsync(conversationId);
    }

    public Task<IEnumerable<IConversation>> GetConversationsByUserIdAsync(string userId)
    {
        return conversationService.GetConversationsByUserIdAsync(userId);
    }

    public Task<IConversation?> GetConversationBetweenUsersAsync(string patientId, string doctorId)
    {
        return conversationService.GetConversationBetweenUsersAsync(patientId, doctorId);
    }

    public Task<IConversation?> UpdateConversationAsync(string conversationId, UpdateConversationRequest request)
    {
        return conversationService.UpdateConversationAsync(conversationId, request);
    }

    public Task<bool> DeleteConversationAsync(string conversationId)
    {
        return conversationService.DeleteConversationAsync(conversationId);
    }
}

public record CreateConversationRequest(string PatientId, string DoctorId);
public record UpdateConversationRequest(string Status, DateTime? LastMessageAt);
