using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Services;

/// <summary>
/// Authentication Service - Business Logic Layer
/// Handles OTP generation, validation, and user authentication
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly Dictionary<string, (string Otp, DateTime Expiration)> _otpStore;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _otpStore = new Dictionary<string, (string, DateTime)>();
    }

    /// <summary>
    /// Generates and sends OTP to user's email
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        // Generate 6-digit OTP
        var otp = GenerateOtp();
        
        // Store OTP with 5-minute expiration
        var expiration = DateTime.UtcNow.AddMinutes(5);
        _otpStore[email.ToLower()] = (otp, expiration);
        
        // TODO: Send OTP via email service
        Console.WriteLine($"OTP for {email}: {otp}");
        
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Verifies OTP and authenticates user (auto-registers if new user)
    /// </summary>
    public async Task<(bool Success, User? User, string? Error)> VerifyOtpAsync(string email, string otp)
    {
        email = email.ToLower();
        
        // Check if OTP exists
        if (!_otpStore.ContainsKey(email))
        {
            return (false, null, "Invalid OTP");
        }
        
        var (storedOtp, expiration) = _otpStore[email];
        
        // Check if OTP is expired
        if (DateTime.UtcNow > expiration)
        {
            _otpStore.Remove(email);
            return (false, null, "OTP expired");
        }
        
        // Verify OTP
        if (storedOtp != otp)
        {
            return (false, null, "Invalid OTP");
        }
        
        // Remove used OTP
        _otpStore.Remove(email);
        
        // Check if user exists
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            // Auto-register new user
            user = new User
            {
                Email = email,
                Role = "patient",
                IsActive = true,
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
        
        return (true, user, null);
    }

    /// <summary>
    /// Generates a random 6-digit OTP
    /// </summary>
    private string GenerateOtp()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
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
