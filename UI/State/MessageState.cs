using AiClinic.Interfaces;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Message State for Blazor (Redux-like pattern)
/// Manages message data, cache, and Supabase CRUD operations
/// </summary>
public class MessageState
{
    private readonly IMessageRepository _repository;
    private List<Message> _messages = new();
    private Message? _selectedMessage;
    private bool _isLoading;
    private string? _errorMessage;

    public MessageState(IMessageRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();
    public Message? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            _selectedMessage = value;
            NotifyStateChanged();
        }
    }
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;

    public async Task<IEnumerable<Message>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var messages = await _repository.GetAllAsync();
            _messages = messages.ToList();
            return messages;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Message>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var message = await _repository.GetByIdAsync(id);
            if (message != null)
            {
                var index = _messages.FindIndex(m => m.Id == id);
                if (index >= 0)
                    _messages[index] = message;
                else
                    _messages.Add(message);
            }
            return message;
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

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var messages = await _repository.GetByConversationIdAsync(conversationId);
            _messages = messages.ToList();
            return messages;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Message>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Message?> GetLatestMessageAsync(Guid conversationId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var message = await _repository.GetLatestMessageAsync(conversationId);
            return message;
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

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var count = await _repository.GetUnreadCountAsync(conversationId, userId);
            return count;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return 0;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task MarkAsReadAsync(Guid messageId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            await _repository.MarkAsReadAsync(messageId);
            
            // Update cache
            var index = _messages.FindIndex(m => m.Id == messageId);
            if (index >= 0)
            {
                var message = _messages[index];
                var updatedMessage = message.WithMarkedAsRead();
                _messages[index] = updatedMessage;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Message?> CreateAsync(Message message)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(message);
            _messages.Add(created);
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

    public async Task<Message?> UpdateAsync(Message message)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(message);
            var index = _messages.FindIndex(m => m.Id == message.Id);
            if (index >= 0)
                _messages[index] = updated;
            if (_selectedMessage?.Id == message.Id)
                _selectedMessage = updated;
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
                _messages.RemoveAll(m => m.Id == id);
                if (_selectedMessage?.Id == id)
                    _selectedMessage = null;
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
        _messages.Clear();
        _selectedMessage = null;
        _errorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
