namespace AiClinic.Interfaces;

/// <summary>
/// Abstraction for Supabase Authentication operations
/// Adapter Pattern: Adapts Supabase Auth API to application interface
/// </summary>
public interface ISupabaseAuthClient
{
    Task<bool> SendOtpAsync(string email);
    Task<(bool Success, string? AccessToken, string? RefreshToken, string? Error)> VerifyOtpAsync(string email, string otp);
    Task<bool> RestoreSessionAsync(string accessToken, string refreshToken);
    Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync();
    Task SignOutAsync();
}

/// <summary>
/// Authentication tokens returned from Supabase
/// </summary>
public class AuthTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
