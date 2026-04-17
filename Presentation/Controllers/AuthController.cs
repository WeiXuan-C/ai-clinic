using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Presentation.State;
using AiClinic.Presentation.Services;
using System.Text.Json;

namespace AiClinic.Presentation.Controllers;

// Adapter Pattern - Adapts application services to presentation layer
// Authentication is handled by Supabase Auth
public class AuthController
{
    private readonly IAuthService _authService;
    private readonly AppState _appState;
    private readonly BrowserStorageService _browserStorage;
    private const string AUTH_TOKEN_KEY = "auth_token";
    private const string USER_DATA_KEY = "user_data";

    public AuthController(
        IAuthService authService,
        AppState appState,
        BrowserStorageService browserStorage)
    {
        _authService = authService;
        _appState = appState;
        _browserStorage = browserStorage;
    }

    public async Task<AuthResponse> SignInWithOtpAsync(string email)
    {
        // Supabase handles OTP generation and sending
        var response = await _authService.SignInWithOtpAsync(email);
        return response;
    }
    
    public async Task<AuthResponse> SignUpWithOtpAsync(string email)
    {
        // Supabase handles OTP generation and sending for new users
        var response = await _authService.SignUpWithOtpAsync(email);
        return response;
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string token)
    {
        // Supabase handles OTP verification
        var response = await _authService.VerifyOtpAsync(email, token);

        if (response.Success && response.Token != null && response.User != null)
        {
            _appState.SetAuthData(response.Token, response.User);
            
            // Save to browser storage for persistence
            await _browserStorage.SetItemAsync(AUTH_TOKEN_KEY, response.Token);
            await _browserStorage.SetItemAsync(USER_DATA_KEY, JsonSerializer.Serialize(response.User));
        }

        return response;
    }
    
    public async Task<AuthResponse> CompleteRegistrationAsync(string email, string fullName, string role)
    {
        // Complete user registration with additional details
        var response = await _authService.CompleteRegistrationAsync(email, fullName, role);
        return response;
    }

    public async Task LogoutAsync()
    {
        await _authService.SignOutAsync();
        _appState.ClearAuthData();
        
        // Clear browser storage
        await _browserStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
        await _browserStorage.RemoveItemAsync(USER_DATA_KEY);
    }

    public bool IsAuthenticated()
    {
        return _appState.IsAuthenticated;
    }

    public UserDto? GetCurrentUser()
    {
        return _appState.CurrentUser;
    }

    /// <summary>
    /// Check if user is signed in using Supabase GetUser
    /// </summary>
    public async Task<bool> IsSignedInAsync()
    {
        return await _authService.IsSignedInAsync();
    }

    /// <summary>
    /// Check authentication status and sync with AppState
    /// Returns true if signed in, and updates AppState with user data
    /// </summary>
    public async Task<bool> CheckAndSyncAuthenticationAsync()
    {
        // First check if we have data in browser storage
        var storedToken = await _browserStorage.GetItemAsync(AUTH_TOKEN_KEY);
        var storedUserData = await _browserStorage.GetItemAsync(USER_DATA_KEY);
        
        if (!string.IsNullOrEmpty(storedToken) && !string.IsNullOrEmpty(storedUserData))
        {
            try
            {
                var storedUser = JsonSerializer.Deserialize<UserDto>(storedUserData);
                if (storedUser != null)
                {
                    // Restore to AppState
                    _appState.SetAuthData(storedToken, storedUser);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing user data: {ex.Message}");
            }
        }
        
        // If not in storage, check Supabase session
        var (isSignedIn, supabaseUser) = await _authService.CheckAuthenticationAsync();
        
        if (isSignedIn && supabaseUser != null)
        {
            // User is authenticated and registered - update state if needed
            if (_appState.CurrentUser == null || _appState.CurrentUser.Id != supabaseUser.Id)
            {
                // Get the current session token
                var token = GetCurrentSessionToken();
                if (token != null)
                {
                    _appState.SetAuthData(token, supabaseUser);
                    
                    // Save to browser storage
                    await _browserStorage.SetItemAsync(AUTH_TOKEN_KEY, token);
                    await _browserStorage.SetItemAsync(USER_DATA_KEY, JsonSerializer.Serialize(supabaseUser));
                }
            }
            return true;
        }
        else if (!isSignedIn)
        {
            // User is not signed in - clear state and storage
            _appState.ClearAuthData();
            await _browserStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
            await _browserStorage.RemoveItemAsync(USER_DATA_KEY);
            return false;
        }
        
        // User is signed in but not registered (verified email only)
        return false;
    }

    /// <summary>
    /// Get current session token from Supabase
    /// </summary>
    private string? GetCurrentSessionToken()
    {
        try
        {
            return _authService.GetCurrentSessionToken();
        }
        catch
        {
            return null;
        }
    }
}
