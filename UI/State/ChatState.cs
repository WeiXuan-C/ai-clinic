using AiClinic.Core.Entities;

namespace AiClinic.UI.State;

/// <summary>
/// Singleton Pattern Implementation
/// Global chat state shared across the entire application
/// </summary>
public class ChatState
{
    private Conversation? _currentConversation;
    private List<Message> _messages;
    private bool _isLoading;
    private string? _errorMessage;

    public ChatState()
    {
        _messages = new List<Message>();
    }

    /// <summary>
    /// Event triggered when chat state changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Currently active conversation
    /// </summary>
    public Conversation? CurrentConversation
    {
        get => _currentConversation;
        set
        {
            _currentConversation = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Messages in the current conversation
    /// </summary>
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    /// <summary>
    /// Whether chat is currently loading
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Current error message (if any)
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Whether a doctor is assigned to current conversation
    /// </summary>
    public bool HasAssignedDoctor => _currentConversation?.AssignedDoctorId != null;

    /// <summary>
    /// Assigned doctor ID (if any)
    /// </summary>
    public Guid? AssignedDoctorId => _currentConversation?.AssignedDoctorId;

    /// <summary>
    /// Sets the messages for the current conversation
    /// </summary>
    public void SetMessages(IEnumerable<Message> messages)
    {
        _messages = messages.ToList();
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a new message to the current conversation
    /// </summary>
    public void AddMessage(Message message)
    {
        _messages.Add(message);
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears the current conversation and messages
    /// </summary>
    public void ClearConversation()
    {
        _currentConversation = null;
        _messages.Clear();
        _errorMessage = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates a message in the list
    /// </summary>
    public void UpdateMessage(Message message)
    {
        var index = _messages.FindIndex(m => m.Id == message.Id);
        if (index >= 0)
        {
            _messages[index] = message;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Gets unread message count
    /// </summary>
    public int GetUnreadCount(Guid userId)
    {
        return _messages.Count(m => !m.IsRead && m.SenderId != userId);
    }

    /// <summary>
    /// Notifies subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
