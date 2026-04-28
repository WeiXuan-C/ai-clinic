using AiClinic.Interfaces;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Conversation State for Blazor (Redux-like pattern)
/// Manages conversation data, cache, and Supabase CRUD operations
/// </summary>
public class ConversationState
{
    private readonly IConversationRepository _repository;
    private List<Conversation> _conversations = new();
    private Conversation? _selectedConversation;
    private bool _isLoading;
    private string? _errorMessage;

    public ConversationState(IConversationRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public IReadOnlyList<Conversation> Conversations => _conversations.AsReadOnly();
    public Conversation? SelectedConversation
    {
        get => _selectedConversation;
        set
        {
            _selectedConversation = value;
            NotifyStateChanged();
        }
    }
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversations = await _repository.GetAllAsync();
            _conversations = conversations.ToList();
            return conversations;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Conversation>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversation = await _repository.GetByIdAsync(id);
            if (conversation != null)
            {
                var index = _conversations.FindIndex(c => c.Id == id);
                if (index >= 0)
                    _conversations[index] = conversation;
                else
                    _conversations.Add(conversation);
            }
            return conversation;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversations = await _repository.GetByPatientIdAsync(patientId);
            _conversations = conversations.ToList();
            return conversations;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Conversation>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversations = await _repository.GetByDoctorIdAsync(doctorId);
            _conversations = conversations.ToList();
            return conversations;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Conversation>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversations = await _repository.GetActiveConversationsAsync();
            _conversations = conversations.ToList();
            return conversations;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Conversation>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Conversation?> GetActiveConversationByPatientIdAsync(Guid patientId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var conversation = await _repository.GetActiveConversationByPatientIdAsync(patientId);
            return conversation;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Conversation?> CreateAsync(Conversation conversation)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(conversation);
            _conversations.Add(created);
            return created;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Conversation?> UpdateAsync(Conversation conversation)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(conversation);
            var index = _conversations.FindIndex(c => c.Id == conversation.Id);
            if (index >= 0)
                _conversations[index] = updated;
            if (_selectedConversation?.Id == conversation.Id)
                _selectedConversation = updated;
            return updated;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _conversations.RemoveAll(c => c.Id == id);
                if (_selectedConversation?.Id == id)
                    _selectedConversation = null;
            }
            return success;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return false;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public void ClearCache()
    {
        _conversations.Clear();
        _selectedConversation = null;
        _errorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
