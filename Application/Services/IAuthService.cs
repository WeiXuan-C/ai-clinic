using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IAuthService
{
    // Supabase Auth OTP methods (Passwordless)
    Task<AuthResponse> SignInWithOtpAsync(string email);
    Task<AuthResponse> SignUpWithOtpAsync(string email);
    Task<AuthResponse> VerifyOtpAsync(string email, string token);
    Task<AuthResponse> CompleteRegistrationAsync(string email, string fullName, string role);
    
    Task SignOutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    
    // Check if user is signed in using Supabase GetUser
    Task<bool> IsSignedInAsync();
    Task<(bool isSignedIn, UserDto? user)> CheckAuthenticationAsync();
    
    // Get current session token
    string? GetCurrentSessionToken();
}
