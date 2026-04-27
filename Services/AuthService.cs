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
    /// Always uses create_user=true because we control user existence via local database
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            email = email.ToLower();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            
            // Use the OTP endpoint directly
            var otpUrl = $"{_supabaseUrl}/auth/v1/otp";
            
            // Always create user in Supabase Auth - we control via local database
            var requestBody = new
            {
                email = email,
                create_user = true,  // Always true - local DB is source of truth
                data = new { }
            };
            
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            var response = await httpClient.PostAsync(otpUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Failed to send OTP to {email}: {responseContent}");
                return false;
            }
            
            Console.WriteLine($"✅ OTP sent to {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending OTP: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
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
            
            // Use direct HTTP call to the Supabase Auth API
            using var httpClient = new HttpClient();
            
            var requestBody = new
            {
                email = email,
                token = otp,
                type = "email"  // For email OTP verification
            };
            
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            
            // Construct the full URL
            var verifyUrl = $"{_supabaseUrl}/auth/v1/verify";
            Console.WriteLine($"🔍 Verifying OTP at: {verifyUrl}");
            Console.WriteLine($"🔍 Email: {email}, OTP length: {otp.Length}");
            
            var response = await httpClient.PostAsync(verifyUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            Console.WriteLine($"📥 Response Content: {responseContent}");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ OTP verification failed for {email}");
                
                // Parse error response for better error messages
                string errorMessage = "Invalid or expired OTP code. Please request a new code.";
                try
                {
                    var errorDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                    
                    // Check for error field
                    if (errorDoc.RootElement.TryGetProperty("error", out var error))
                    {
                        var errorText = error.GetString()?.ToLower() ?? "";
                        Console.WriteLine($"❌ Error field: {errorText}");
                        
                        if (errorText.Contains("expired"))
                        {
                            errorMessage = "OTP code has expired. Please click 'Resend Code' to get a new one.";
                        }
                        else if (errorText.Contains("invalid") || errorText.Contains("not found"))
                        {
                            errorMessage = "Invalid OTP code. Please check your email and enter the correct code.";
                        }
                        else if (errorText.Contains("otp"))
                        {
                            errorMessage = "Invalid OTP code. Please try again.";
                        }
                    }
                    
                    // Check for error_description field
                    if (errorDoc.RootElement.TryGetProperty("error_description", out var errorDesc))
                    {
                        var errorDescText = errorDesc.GetString() ?? "";
                        Console.WriteLine($"❌ Error description: {errorDescText}");
                        if (!string.IsNullOrEmpty(errorDescText))
                        {
                            errorMessage = errorDescText;
                        }
                    }
                    
                    // Check for msg field
                    if (errorDoc.RootElement.TryGetProperty("msg", out var msg))
                    {
                        var msgText = msg.GetString() ?? "";
                        Console.WriteLine($"❌ Message: {msgText}");
                        if (!string.IsNullOrEmpty(msgText))
                        {
                            errorMessage = msgText;
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"❌ Error parsing response: {parseEx.Message}");
                }
                
                return (false, null, errorMessage, null, null);
            }
            
            // Parse the response to get session info
            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            
            string? accessToken = null;
            string? refreshToken = null;
            string? userId = null;
            
            // Try to get access_token
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var accessTokenProp))
            {
                accessToken = accessTokenProp.GetString();
            }
            
            // Try to get refresh_token
            if (jsonDoc.RootElement.TryGetProperty("refresh_token", out var refreshTokenProp))
            {
                refreshToken = refreshTokenProp.GetString();
            }
            
            // Try to get user id
            if (jsonDoc.RootElement.TryGetProperty("user", out var userProp))
            {
                if (userProp.TryGetProperty("id", out var userIdProp))
                {
                    userId = userIdProp.GetString();
                }
            }

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
            var updatedUser = user.WithUpdatedLogin();
            await _userRepository.UpdateAsync(updatedUser);
            Console.WriteLine($"✅ Existing user logged in: {email}");
            
            return (true, updatedUser, null, accessToken, refreshToken);
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
            
            // Create user record using factory method
            var user = User.Create(email, fullName, role);
            
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
