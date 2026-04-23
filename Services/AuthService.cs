using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Microsoft.Extensions.Configuration;
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
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public AuthService(IUserRepository userRepository, SupabaseClient supabase, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _supabase = supabase;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
        _supabaseKey = configuration["Supabase:Key"] ?? throw new Exception("Supabase Key not configured");
        
        Console.WriteLine($"🔧 AuthService initialized with URL: {_supabaseUrl}");
    }

    /// <summary>
    /// Sends OTP to user's email using Supabase Auth
    /// Uses SendMagicLink which sends an OTP code when the email template contains {{ .Token }}
    /// 
    /// CRITICAL: Your Supabase Magic Link email template MUST contain ONLY {{ .Token }}
    /// and NO {{ .ConfirmationURL }} or clickable links to avoid email prefetching issues
    /// 
    /// Email prefetching by security tools (like Microsoft Defender) will consume magic links
    /// before users can click them, causing "invalid OTP" errors. Using OTP codes prevents this.
    /// </summary>
    public async Task<bool> SendOtpAsync(string email, bool shouldCreateUser = true)
    {
        try
        {
            // Use SendMagicLink - when email template contains only {{ .Token }}, it sends OTP code
            // The key is that the email template must NOT have {{ .ConfirmationURL }}
            var options = new Supabase.Gotrue.SignInOptions
            {
                // Don't set RedirectTo - we're using OTP codes, not magic links
                RedirectTo = null
            };
            
            // Note: C# library doesn't have shouldCreateUser option in SendMagicLink
            // By default, it will create users if they don't exist
            // For signin-only flow, we check user existence before calling this method
            var result = await _supabase.Auth.SendMagicLink(email, options);
            
            Console.WriteLine($"✅ OTP sent to {email} via Supabase Auth");
            Console.WriteLine($"⚠️  IMPORTANT: Ensure your Supabase Magic Link email template contains ONLY {{ .Token }} and NO {{ .ConfirmationURL }}!");
            Console.WriteLine($"📧 Email template should be: <h2>Your verification code</h2><p>Enter this code: {{ .Token }}</p>");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending OTP: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifies OTP using Supabase Auth
    /// Uses direct HTTP call to Supabase API for email OTP verification
    /// NOTE: This only verifies the OTP and returns the session.
    /// For signup: User creation happens AFTER role selection.
    /// For signin: Returns existing user from local database.
    /// </summary>
    public async Task<(bool Success, User? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            email = email.ToLower();
            
            // The C# library's VerifyOTP doesn't support email OTP properly
            // We need to make a direct HTTP call to the Supabase Auth API
            using var httpClient = new HttpClient();
            
            var requestBody = new
            {
                email = email,
                token = otp,
                type = "email"  // Use "email" type for email OTP verification
            };
            
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            
            // Construct the full URL - Supabase URL format is: https://xxx.supabase.co
            var verifyUrl = $"{_supabaseUrl}/auth/v1/verify";
            Console.WriteLine($"🔍 Verifying OTP at: {verifyUrl}");
            
            var response = await httpClient.PostAsync(verifyUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ OTP verification failed for {email}: {responseContent}");
                return (false, null, "Invalid or expired OTP code. Please request a new code.", null, null);
            }
            
            // Parse the response to get session info
            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
            var refreshToken = jsonDoc.RootElement.GetProperty("refresh_token").GetString();
            var userId = jsonDoc.RootElement.GetProperty("user").GetProperty("id").GetString();

            Console.WriteLine($"✅ OTP verified successfully for {email}");
            Console.WriteLine($"✅ Session created - Access Token: {(accessToken != null ? "Present" : "Missing")}");
            Console.WriteLine($"✅ Supabase Auth User ID: {userId}");

            // Check if user exists in local database
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                // New user - OTP verified but user record not created yet
                // This is expected for signup flow - user will be created after role selection
                Console.WriteLine($"📝 New user verified: {email} - User record will be created after role selection");
                return (true, null, null, accessToken, refreshToken);
            }

            // Existing user - signin flow
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            Console.WriteLine($"✅ Existing user logged in: {email}");
            
            return (true, user, null, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error verifying OTP: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            
            // Provide more specific error messages
            var errorMessage = ex.Message.ToLower() switch
            {
                var msg when msg.Contains("expired") => "OTP code has expired. Please click 'Resend Code' to get a new one.",
                var msg when msg.Contains("invalid") => "Invalid OTP code. Please check your email and try again.",
                var msg when msg.Contains("too many") => "Too many attempts. Please wait a few minutes and try again.",
                var msg when msg.Contains("403") => "OTP code has expired or is invalid. Please request a new code.",
                var msg when msg.Contains("uri") => "Configuration error. Please check Supabase URL settings.",
                _ => $"Verification failed: {ex.Message}"
            };
            
            return (false, null, errorMessage, null, null);
        }
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        try
        {
            await _supabase.Auth.SetSession(accessToken, refreshToken);
            
            // Check if session is valid
            var currentUser = _supabase.Auth.CurrentUser;
            return currentUser != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error restoring session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        try
        {
            var session = await _supabase.Auth.RefreshSession();
            
            if (session?.AccessToken != null)
            {
                return (true, session.AccessToken, session.RefreshToken);
            }
            
            return (false, null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error refreshing session: {ex.Message}");
            return (false, null, null);
        }
    }

    /// <summary>
    /// Signs out the current user from Supabase
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            await _supabase.Auth.SignOut();
            Console.WriteLine("✅ User signed out from Supabase");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error signing out: {ex.Message}");
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
    /// Creates a new user with role and profile
    /// This should only be called AFTER OTP verification and role selection
    /// </summary>
    public async Task<User?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        try
        {
            email = email.ToLower();
            
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                Console.WriteLine($"⚠️  User already exists: {email}");
                return existingUser;
            }
            
            // Create user record
            var user = new User
            {
                Email = email,
                FullName = fullName,
                Role = role.ToLower(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            
            var createdUser = await _userRepository.AddAsync(user);
            Console.WriteLine($"✅ User created: {email} with role: {role}");
            
            return createdUser;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating user: {ex.Message}");
            return null;
        }
    }
}
