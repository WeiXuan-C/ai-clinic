using AiClinic.Application.DTOs;

namespace AiClinic.Presentation.State;

// Singleton Pattern - State Management
public class AppState
{
    private static AppState? _instance;
    private static readonly object _lock = new();

    private UserDto? _currentUser;
    private string? _authToken;
    private PatientProfileDto? _currentPatientProfile;
    private List<ActivityLogDto> _recentActivityLogs = new();

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

    public PatientProfileDto? CurrentPatientProfile
    {
        get => _currentPatientProfile;
        set
        {
            _currentPatientProfile = value;
            OnPatientProfileChanged?.Invoke();
            OnStateChanged?.Invoke();
        }
    }

    public List<ActivityLogDto> RecentActivityLogs
    {
        get => _recentActivityLogs;
        set
        {
            _recentActivityLogs = value;
            OnActivityLogsChanged?.Invoke();
            OnStateChanged?.Invoke();
        }
    }

    public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(AuthToken);

    public bool HasPatientProfile => CurrentPatientProfile != null;

    // Events
    public event Action? OnStateChanged;
    public event Action? OnPatientProfileChanged;
    public event Action? OnActivityLogsChanged;

    // Auth Methods
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
        _currentPatientProfile = null;
        _recentActivityLogs.Clear();
        OnStateChanged?.Invoke();
    }

    // Patient Profile Methods
    public void UpdatePatientProfile(PatientProfileDto profile)
    {
        _currentPatientProfile = profile;
        OnPatientProfileChanged?.Invoke();
        OnStateChanged?.Invoke();
    }

    public void ClearPatientProfile()
    {
        _currentPatientProfile = null;
        OnPatientProfileChanged?.Invoke();
        OnStateChanged?.Invoke();
    }

    // Activity Log Methods
    public void AddActivityLog(ActivityLogDto log)
    {
        _recentActivityLogs.Insert(0, log);
        if (_recentActivityLogs.Count > 50)
        {
            _recentActivityLogs = _recentActivityLogs.Take(50).ToList();
        }
        OnActivityLogsChanged?.Invoke();
        OnStateChanged?.Invoke();
    }

    public void SetActivityLogs(IEnumerable<ActivityLogDto> logs)
    {
        _recentActivityLogs = logs.ToList();
        OnActivityLogsChanged?.Invoke();
        OnStateChanged?.Invoke();
    }

    public void ClearActivityLogs()
    {
        _recentActivityLogs.Clear();
        OnActivityLogsChanged?.Invoke();
        OnStateChanged?.Invoke();
    }
}
