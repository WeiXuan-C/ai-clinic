using ai_clinic.Interfaces;

namespace ai_clinic.UI.State;

/// <summary>
/// Scoped Authentication State for Blazor (Redux-like pattern)
/// ONLY manages state - NO business logic, NO DAO calls, NO API calls
/// </summary>
public class AuthState
{
    private User? _currentUser;
    private bool _isAuthenticated;
    private string? _accessToken;
    private string? _refreshToken;
    private bool _isLoading;
    private string? _errorMessage;

    /// <summary>
    /// Event triggered when authentication state changes
    /// Components should subscribe to this and call StateHasChanged()
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Currently authenticated user
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Access token for Supabase authentication
    /// </summary>
    public string? AccessToken
    {
        get => _accessToken;
        set
        {
            _accessToken = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Refresh token for Supabase authentication
    /// </summary>
    public string? RefreshToken
    {
        get => _refreshToken;
        set
        {
            _refreshToken = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Loading state indicator
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
    /// Error message if any operation fails
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
    /// Current user's role
    /// </summary>
    public string? UserRole => _currentUser?.Role;

    /// <summary>
    /// Current user's ID
    /// </summary>
    public Guid? UserId => _currentUser?.Id;

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return _currentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if current user is a patient
    /// </summary>
    public bool IsPatient => HasRole("patient");

    /// <summary>
    /// Checks if current user is a doctor
    /// </summary>
    public bool IsDoctor => HasRole("doctor");

    /// <summary>
    /// Checks if current user is an admin
    /// </summary>
    public bool IsAdmin => HasRole("admin");

    /// <summary>
    /// Sets authentication state with user and tokens
    /// </summary>
    public void SetAuthentication(User user, string? accessToken, string? refreshToken)
    {
        _currentUser = user;
        _isAuthenticated = true;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        NotifyStateChanged();
    }

    /// <summary>
    /// Logs out the current user (clears all state)
    /// </summary>
    public void Logout()
    {
        _currentUser = null;
        _isAuthenticated = false;
        _accessToken = null;
        _refreshToken = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears error message
    /// </summary>
    public void ClearError()
    {
        _errorMessage = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
