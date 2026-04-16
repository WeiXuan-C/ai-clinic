using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly SupabaseContext _context;

    public AuthService(
        IUserRepository userRepository,
        SupabaseContext context)
    {
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<AuthResponse> SignInWithOtpAsync(string email)
    {
        try
        {
            // Supabase Auth handles OTP generation and email sending
            await _context.Client.Auth.SignIn(Supabase.Gotrue.Constants.SignInType.Email, email);
            
            return new AuthResponse(true, null, "OTP sent to your email", null);
        }
        catch (Exception ex)
        {
            return new AuthResponse(false, null, $"Failed to send OTP: {ex.Message}", null);
        }
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string token)
    {
        try
        {
            // Supabase Auth handles OTP verification
            var session = await _context.Client.Auth.VerifyOTP(email, token, Supabase.Gotrue.Constants.EmailOtpType.Email);
            
            if (session?.User == null)
            {
                return new AuthResponse(false, null, "Invalid or expired OTP", null);
            }

            // Check if user exists in our database, if not create (auto-registration)
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.Parse(session.User.Id),
                    Email = email,
                    Role = "patient",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                user = await _userRepository.AddAsync(user);
            }
            else
            {
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }

            var userDto = new UserDto(user.Id, user.Email, null, user.Role);
            
            return new AuthResponse(true, session.AccessToken, "Login successful", userDto);
        }
        catch (Exception ex)
        {
            return new AuthResponse(false, null, $"Verification failed: {ex.Message}", null);
        }
    }

    public async Task SignOutAsync()
    {
        await _context.Client.Auth.SignOut();
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var supabaseUser = _context.Client.Auth.CurrentUser;
        
        if (supabaseUser == null)
            return null;

        var user = await _userRepository.GetByEmailAsync(supabaseUser.Email ?? "");
        
        if (user == null)
            return null;

        return new UserDto(user.Id, user.Email, null, user.Role);
    }
}
