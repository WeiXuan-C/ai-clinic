using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IAuthService
{
    Task<bool> SendOtpAsync(string email);
    Task<AuthResponse> VerifyOtpAsync(string email, string code);
    Task<string> GenerateJwtTokenAsync(Guid userId, string email, string role);
}
