using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Presentation.State;

namespace AiClinic.Presentation.Controllers;

// Adapter Pattern - Adapts application services to presentation layer
// Authentication is handled by Supabase Auth
public class AuthController
{
    private readonly IAuthService _authService;
    private readonly AppState _appState;

    public AuthController(
        IAuthService authService,
        AppState appState)
    {
        _authService = authService;
        _appState = appState;
    }

    public async Task<AuthResponse> SignInWithOtpAsync(string email)
    {
        // Supabase handles OTP generation and sending
        var response = await _authService.SignInWithOtpAsync(email);
        return response;
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string token)
    {
        // Supabase handles OTP verification
        var response = await _authService.VerifyOtpAsync(email, token);

        if (response.Success && response.Token != null && response.User != null)
        {
            _appState.SetAuthData(response.Token, response.User);
        }

        return response;
    }

    public async Task LogoutAsync()
    {
        await _authService.SignOutAsync();
        _appState.ClearAuthData();
    }

    public bool IsAuthenticated()
    {
        return _appState.IsAuthenticated;
    }

    public UserDto? GetCurrentUser()
    {
        return _appState.CurrentUser;
    }
}
