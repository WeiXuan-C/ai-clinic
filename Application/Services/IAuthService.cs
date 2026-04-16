using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IAuthService
{
    // Supabase Auth OTP methods
    Task<AuthResponse> SignInWithOtpAsync(string email);
    Task<AuthResponse> VerifyOtpAsync(string email, string token);
    Task SignOutAsync();
    Task<UserDto?> GetCurrentUserAsync();
}
