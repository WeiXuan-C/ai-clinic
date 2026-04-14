using AiClinic.Application.DTOs;

namespace AiClinic.Presentation.State;

// Singleton Pattern - State Management
public class AppState
{
    private static AppState? _instance;
    private static readonly object _lock = new();

    private UserDto? _currentUser;
    private string? _authToken;

    private AppState() { }

    public static AppState Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AppState();
                }
            }
            return _instance;
        }
    }

    public UserDto? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            OnStateChanged?.Invoke();
        }
    }

    public string? AuthToken
    {
        get => _authToken;
        set
        {
            _authToken = value;
            OnStateChanged?.Invoke();
        }
    }

    public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(AuthToken);

    public event Action? OnStateChanged;

    public void SetAuthData(string token, UserDto user)
    {
        _authToken = token;
        _currentUser = user;
        OnStateChanged?.Invoke();
    }

    public void ClearAuthData()
    {
        _authToken = null;
        _currentUser = null;
        OnStateChanged?.Invoke();
    }
}
