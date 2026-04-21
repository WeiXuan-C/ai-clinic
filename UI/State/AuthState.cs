using AiClinic.Core.Entities;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Authentication State for Blazor
/// Each user session gets its own instance
/// </summary>
public class AuthState
{
    private User? _currentUser;
    private bool _isAuthenticated;

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
            _isAuthenticated = value != null;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
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
    /// Logs out the current user
    /// </summary>
    public void Logout()
    {
        _currentUser = null;
        _isAuthenticated = false;
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
