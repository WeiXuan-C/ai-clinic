using ai_clinic.Interfaces;
using ai_clinic.UI.State;

namespace ai_clinic.Services;

/// <summary>
/// Conversation Service - Business Logic Layer
/// Handles conversation operations through state management
/// </summary>
public class ConversationService
{
    private readonly ConversationState _state;

    public ConversationService(ConversationState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets all conversations
    /// </summary>
    public async Task<IEnumerable<IConversation>> GetAllConversationsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a conversation by ID
    /// </summary>
    public async Task<IConversation?> GetConversationByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets conversations by patient ID
    /// </summary>
    public async Task<IEnumerable<IConversation>> GetPatientConversationsAsync(Guid patientId)
    {
        return await _state.GetByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Gets conversations by doctor ID
    /// </summary>
    public async Task<IEnumerable<IConversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return await _state.GetByDoctorIdAsync(doctorId);
    }

    /// <summary>
    /// Gets all active conversations
    /// </summary>
    public async Task<IEnumerable<IConversation>> GetActiveConversationsAsync()
    {
        return await _state.GetActiveConversationsAsync();
    }

    /// <summary>
    /// Gets active conversation for a patient
    /// </summary>
    public async Task<IConversation?> GetActivePatientConversationAsync(Guid patientId)
    {
        return await _state.GetActiveConversationByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Creates a new conversation
    /// </summary>
    public async Task<IConversation?> CreateConversationAsync(IConversation conversation)
    {
        var concreteConversation = conversation as Conversation ?? new Conversation
        {
            Id = conversation.Id,
            PatientId = conversation.PatientId,
            AssignedDoctorId = conversation.AssignedDoctorId,
            Title = conversation.Title,
            Status = conversation.Status,
            InitialSymptoms = conversation.InitialSymptoms,
            AiSuggestedSpecialization = conversation.AiSuggestedSpecialization,
            StartedAt = conversation.StartedAt,
            ClosedAt = conversation.ClosedAt,
            LastMessageAt = conversation.LastMessageAt,
            TotalMessages = conversation.TotalMessages,
            AiMessagesCount = conversation.AiMessagesCount,
            DoctorMessagesCount = conversation.DoctorMessagesCount,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            ConsultationStatus = conversation.ConsultationStatus,
            DiagnosisCompleted = conversation.DiagnosisCompleted,
            PrescriptionGenerated = conversation.PrescriptionGenerated,
            RequiredSpecialization = conversation.RequiredSpecialization,
            AiConfidenceScore = conversation.AiConfidenceScore
        };
        return await _state.CreateAsync(concreteConversation);
    }

    /// <summary>
    /// Updates a conversation
    /// </summary>
    public async Task<IConversation?> UpdateConversationAsync(IConversation conversation)
    {
        var concreteConversation = conversation as Conversation ?? new Conversation
        {
            Id = conversation.Id,
            PatientId = conversation.PatientId,
            AssignedDoctorId = conversation.AssignedDoctorId,
            Title = conversation.Title,
            Status = conversation.Status,
            InitialSymptoms = conversation.InitialSymptoms,
            AiSuggestedSpecialization = conversation.AiSuggestedSpecialization,
            StartedAt = conversation.StartedAt,
            ClosedAt = conversation.ClosedAt,
            LastMessageAt = conversation.LastMessageAt,
            TotalMessages = conversation.TotalMessages,
            AiMessagesCount = conversation.AiMessagesCount,
            DoctorMessagesCount = conversation.DoctorMessagesCount,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            ConsultationStatus = conversation.ConsultationStatus,
            DiagnosisCompleted = conversation.DiagnosisCompleted,
            PrescriptionGenerated = conversation.PrescriptionGenerated,
            RequiredSpecialization = conversation.RequiredSpecialization,
            AiConfidenceScore = conversation.AiConfidenceScore
        };
        return await _state.UpdateAsync(concreteConversation);
    }

    /// <summary>
    /// Deletes a conversation
    /// </summary>
    public async Task<bool> DeleteConversationAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets cached conversations from state
    /// </summary>
    public IReadOnlyList<IConversation> GetCachedConversations()
    {
        return _state.Conversations.Cast<IConversation>().ToList();
    }

    /// <summary>
    /// Gets the currently selected conversation
    /// </summary>
    public IConversation? GetSelectedConversation()
    {
        return _state.SelectedConversation;
    }

    /// <summary>
    /// Sets the selected conversation
    /// </summary>
    public void SetSelectedConversation(IConversation? conversation)
    {
        _state.SelectedConversation = conversation as Conversation;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IConversation?> CreateConversationAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var patientId = Guid.Parse(requestType.GetProperty("PatientId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var title = requestType.GetProperty("Title")?.GetValue(request)?.ToString();
        
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Title = title,
            Status = "active",
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConsultationStatus = "pending"
        };
        
        return await CreateConversationAsync((IConversation)conversation);
    }
    
    public async Task<IConversation?> GetConversationByIdAsync(string conversationId)
    {
        if (Guid.TryParse(conversationId, out var guid))
        {
            return await GetConversationByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<IEnumerable<IConversation>> GetConversationsByUserIdAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return await GetPatientConversationsAsync(guid);
        }
        return Enumerable.Empty<IConversation>();
    }
    
    public async Task<IConversation?> GetConversationBetweenUsersAsync(string userId1, string userId2)
    {
        // Placeholder implementation
        return null;
    }
    
    public async Task<IConversation?> UpdateConversationAsync(string conversationId, object updates)
    {
        if (!Guid.TryParse(conversationId, out var guid))
        {
            return null;
        }
        
        var existing = await GetConversationByIdAsync(guid);
        if (existing == null)
        {
            return null;
        }
        
        // Extract properties from updates object dynamically
        var updatesType = updates.GetType();
        var status = updatesType.GetProperty("Status")?.GetValue(updates)?.ToString() ?? existing.Status;
        
        var updated = new Conversation
        {
            Id = guid,
            PatientId = existing.PatientId,
            AssignedDoctorId = existing.AssignedDoctorId,
            Title = existing.Title,
            Status = status,
            InitialSymptoms = existing.InitialSymptoms,
            AiSuggestedSpecialization = existing.AiSuggestedSpecialization,
            StartedAt = existing.StartedAt,
            ClosedAt = status == "closed" ? DateTime.UtcNow : existing.ClosedAt,
            LastMessageAt = existing.LastMessageAt,
            TotalMessages = existing.TotalMessages,
            AiMessagesCount = existing.AiMessagesCount,
            DoctorMessagesCount = existing.DoctorMessagesCount,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            ConsultationStatus = existing.ConsultationStatus,
            DiagnosisCompleted = existing.DiagnosisCompleted,
            PrescriptionGenerated = existing.PrescriptionGenerated,
            RequiredSpecialization = existing.RequiredSpecialization,
            AiConfidenceScore = existing.AiConfidenceScore
        };
        
        return await UpdateConversationAsync(updated);
    }
    
    public async Task<bool> DeleteConversationAsync(string conversationId)
    {
        if (Guid.TryParse(conversationId, out var guid))
        {
            return await DeleteConversationAsync(guid);
        }
        return false;
    }
}
