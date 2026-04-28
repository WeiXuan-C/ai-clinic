using AiClinic.UI.State;

namespace AiClinic.Services;

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
    public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a conversation by ID
    /// </summary>
    public async Task<Conversation?> GetConversationByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets conversations by patient ID
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetPatientConversationsAsync(Guid patientId)
    {
        return await _state.GetByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Gets conversations by doctor ID
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return await _state.GetByDoctorIdAsync(doctorId);
    }

    /// <summary>
    /// Gets all active conversations
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync()
    {
        return await _state.GetActiveConversationsAsync();
    }

    /// <summary>
    /// Gets active conversation for a patient
    /// </summary>
    public async Task<Conversation?> GetActivePatientConversationAsync(Guid patientId)
    {
        return await _state.GetActiveConversationByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Creates a new conversation
    /// </summary>
    public async Task<Conversation?> CreateConversationAsync(Conversation conversation)
    {
        return await _state.CreateAsync(conversation);
    }

    /// <summary>
    /// Updates a conversation
    /// </summary>
    public async Task<Conversation?> UpdateConversationAsync(Conversation conversation)
    {
        return await _state.UpdateAsync(conversation);
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
    public IReadOnlyList<Conversation> GetCachedConversations()
    {
        return _state.Conversations;
    }

    /// <summary>
    /// Gets the currently selected conversation
    /// </summary>
    public Conversation? GetSelectedConversation()
    {
        return _state.SelectedConversation;
    }

    /// <summary>
    /// Sets the selected conversation
    /// </summary>
    public void SetSelectedConversation(Conversation? conversation)
    {
        _state.SelectedConversation = conversation;
    }
}
