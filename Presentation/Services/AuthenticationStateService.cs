using AiClinic.Application.Services;
using AiClinic.Presentation.State;

namespace AiClinic.Presentation.Services;

/// <summary>
/// Service to manage authentication state across the application
/// Checks authentication once on startup and maintains state
/// </summary>
public class AuthenticationStateService
{
    private readonly IAuthService _authService;
    private readonly AppState _appState;
    private bool _isInitialized = false;

    public AuthenticationStateService(IAuthService authService, AppState appState)
    {
        _authService = authService;
        _appState = appState;
    }

    /// <summary>
    /// Initialize authentication state on app startup
    /// This should be called once when the app starts
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            // Check if user has a valid session
            var (isSignedIn, user) = await _authService.CheckAuthenticationAsync();

            if (isSignedIn && user != null)
            {
                // User is authenticated - update state
                var token = _authService.GetCurrentSessionToken();
                if (token != null)
                {
                    _appState.SetAuthData(token, user);
                }
            }
            else
            {
                // No valid session - clear state
                _appState.ClearAuthData();
            }

            _isInitialized = true;
        }
        catch
        {
            // If initialization fails, clear state
            _appState.ClearAuthData();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Check if user is authenticated (uses cached state, no API call)
    /// </summary>
    public bool IsAuthenticated()
    {
        return _appState.IsAuthenticated;
    }

    /// <summary>
    /// Force refresh authentication state (calls GetUser API)
    /// Use this sparingly - only when you need to verify current session
    /// </summary>
    public async Task<bool> RefreshAuthenticationAsync()
    {
        try
        {
            var (isSignedIn, user) = await _authService.CheckAuthenticationAsync();

            if (isSignedIn && user != null)
            {
                var token = _authService.GetCurrentSessionToken();
                if (token != null)
                {
                    _appState.SetAuthData(token, user);
                }
                return true;
            }
            else
            {
                _appState.ClearAuthData();
                return false;
            }
        }
        catch
        {
            _appState.ClearAuthData();
            return false;
        }
    }

    /// <summary>
    /// Check if session might be expired (based on last activity)
    /// Returns true if we should verify the session
    /// </summary>
    public bool ShouldVerifySession()
    {
        // Add logic here to check if enough time has passed
        // For example, verify every 30 minutes
        // This is a placeholder - implement based on your needs
        return false;
    }
}
