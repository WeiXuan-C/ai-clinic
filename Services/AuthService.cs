using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using SupabaseClient = Supabase.Client;

namespace AiClinic.Services;

/// <summary>
/// Authentication Service - Business Logic Layer
/// Handles OTP generation, validation, and user authentication using Supabase Auth
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly SupabaseClient _supabase;

    public AuthService(IUserRepository userRepository, SupabaseClient supabase)
    {
        _userRepository = userRepository;
        _supabase = supabase;
    }

    /// <summary>
    /// Sends OTP to user's email using Supabase Auth
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            // Use Supabase Auth to send OTP email
            var options = new Supabase.Gotrue.SignInWithPasswordlessEmailOptions(email);

            await _supabase.Auth.SignInWithOtp(options);
            
            Console.WriteLine($"✅ OTP sent to {email} via Supabase Auth");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending OTP: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifies OTP using Supabase Auth and syncs with local database
    /// </summary>
    public async Task<(bool Success, User? User, string? Error)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            email = email.ToLower();
            
            // Verify OTP with Supabase Auth
            var response = await _supabase.Auth.VerifyOTP(email, otp, Supabase.Gotrue.Constants.EmailOtpType.MagicLink);
            
            if (response?.User == null)
            {
                return (false, null, "Invalid OTP or verification failed");
            }

            // Get or create user in local database
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                // Create new user in local database
                user = new User
                {
                    Email = email,
                    Role = "patient",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                
                user = await _userRepository.AddAsync(user);
                Console.WriteLine($"✅ New user created: {email}");
            }
            else
            {
                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                Console.WriteLine($"✅ User logged in: {email}");
            }
            
            return (true, user, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error verifying OTP: {ex.Message}");
            return (false, null, $"Verification failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    /// <summary>
    /// Gets user by email
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    public async Task<User> UpdateUserAsync(User user)
    {
        return await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    public async Task<User?> CreateUserAsync(string email, string fullName, string role)
    {
        try
        {
            var user = new User
            {
                Email = email,
                FullName = fullName,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            
            return await _userRepository.AddAsync(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating user: {ex.Message}");
            return null;
        }
    }
}
