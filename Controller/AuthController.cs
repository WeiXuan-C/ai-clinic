using AiClinic.Core.Entities;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies authentication flows by coordinating AuthService and AuthState
/// </summary>
public class AuthController
{
    private readonly AuthService _authService;
    private readonly AuthState _authState;

    public AuthController(AuthService authService, AuthState authState)
    {
        _authService = authService;
        _authState = authState;
    }

    /// <summary>
    /// Initiates login by sending OTP to user's email
    /// Only works for users who are already registered IN LOCAL DATABASE
    /// </summary>
    public async Task<(bool Success, string Message)> InitiateLoginAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email is required");
            }

            if (!IsValidEmail(email))
            {
                return (false, "Invalid email format");
            }

            // Check if user exists IN LOCAL DATABASE (not Supabase Auth)
            Console.WriteLine($"🔍 SIGNIN: Checking if {email} exists in local users table...");
            var userExists = await _authService.GetUserByEmailAsync(email);
            
            if (userExists == null)
            {
                Console.WriteLine($"❌ SIGNIN: User {email} NOT found in local database. Blocking signin.");
                return (false, "This email is not registered. Please sign up first.");
            }
            
            Console.WriteLine($"✅ SIGNIN: User {email} found in local database. Proceeding with signin.");

            // Send OTP - Supabase Auth user may or may not exist, but we don't care
            var success = await _authService.SendOtpAsync(email);

            if (success)
            {
                return (true, "Verification code sent to your email. Please check your inbox.");
            }

            return (false, "Failed to send verification code. Please try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SIGNIN ERROR: {ex.Message}");
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Completes login by verifying OTP and updating auth state
    /// Facade method that coordinates OTP verification and state management
    /// </summary>
    public async Task<(bool Success, string Message)> CompleteLoginAsync(string email, string otp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return (false, "Email and OTP are required");
            }

            var (success, user, error, accessToken, refreshToken) = await _authService.VerifyOtpAsync(email, otp);

            if (!success || user == null)
            {
                return (false, error ?? "Invalid OTP");
            }

            // Update global auth state with tokens
            _authState.SetAuthentication(user, accessToken, refreshToken);

            return (true, "Login successful!");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Initiates signup by sending OTP to user's email
    /// Only works for new users who are not yet registered IN LOCAL DATABASE
    /// </summary>
    public async Task<AuthResponse> SignUpWithOtpAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new AuthResponse { Success = false, Message = "Email is required" };
            }

            if (!IsValidEmail(email))
            {
                return new AuthResponse { Success = false, Message = "Invalid email format" };
            }

            // Check if user already exists IN LOCAL DATABASE (not Supabase Auth)
            Console.WriteLine($"🔍 SIGNUP: Checking if {email} exists in local users table...");
            var existingUser = await _authService.GetUserByEmailAsync(email);
            
            if (existingUser != null)
            {
                Console.WriteLine($"❌ SIGNUP: User {email} already exists in local database. Blocking signup.");
                return new AuthResponse { Success = false, Message = "This email is already registered. Please sign in instead." };
            }
            
            Console.WriteLine($"✅ SIGNUP: User {email} NOT found in local database. Proceeding with signup.");

            // Send OTP - will create Supabase Auth user if doesn't exist
            var success = await _authService.SendOtpAsync(email);

            if (success)
            {
                return new AuthResponse { Success = true, Message = "Verification code sent to your email. Please check your inbox." };
            }

            return new AuthResponse { Success = false, Message = "Failed to send verification code. Please try again." };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ SIGNUP ERROR: {ex.Message}");
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Verifies OTP for signup
    /// </summary>
    public async Task<AuthResponse> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return new AuthResponse { Success = false, Message = "Email and OTP are required" };
            }

            Console.WriteLine($"🔍 VERIFY OTP: Starting verification for {email}");
            var (success, user, error, accessToken, refreshToken) = await _authService.VerifyOtpAsync(email, otp);

            if (!success)
            {
                Console.WriteLine($"❌ VERIFY OTP: Verification failed - {error}");
                return new AuthResponse { Success = false, Message = error ?? "Invalid OTP" };
            }

            Console.WriteLine($"✅ VERIFY OTP: Verification successful. User in local DB: {(user != null ? "Yes" : "No")}");

            // For signup: user will be null (new user, not in local DB yet)
            // For signin: user will be populated (existing user in local DB)
            if (user != null)
            {
                // Existing user trying to signup - should have been blocked earlier
                Console.WriteLine($"⚠️ VERIFY OTP: User already exists in local DB. This is a signin, not signup.");
                _authState.SetAuthentication(user, accessToken, refreshToken);
                return new AuthResponse { Success = true, Message = "OTP verified successfully", User = user };
            }
            
            // New user - OTP verified, proceed to role selection
            Console.WriteLine($"✅ VERIFY OTP: New user verified. Proceeding to role selection.");
            // Store tokens temporarily (we'll set full auth after profile completion)
            _authState.SetAuthentication(null, accessToken, refreshToken);
            return new AuthResponse { Success = true, Message = "OTP verified successfully", User = null };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ VERIFY OTP ERROR: {ex.Message}");
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Completes registration after OTP verification
    /// Creates user record and appropriate profile (patient or doctor)
    /// </summary>
    public async Task<AuthResponse> CompleteRegistrationAsync(string email, string fullName, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(role))
            {
                return new AuthResponse { Success = false, Message = "All fields are required" };
            }

            // Normalize role
            role = role.ToLower();
            if (role != "patient" && role != "doctor")
            {
                return new AuthResponse { Success = false, Message = "Invalid role. Must be 'patient' or 'doctor'" };
            }

            // Check if user already exists
            var existingUser = await _authService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResponse { Success = false, Message = "This account is already registered. Please sign in instead." };
            }

            // Create user record with role
            var user = await _authService.CreateUserWithProfileAsync(email, fullName, role);
            
            if (user == null)
            {
                return new AuthResponse { Success = false, Message = "Failed to create user account" };
            }

            // Update auth state
            _authState.CurrentUser = user;

            return new AuthResponse { Success = true, Message = "Registration completed successfully", User = user };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Logs out the current user
    /// Facade method that clears auth state
    /// </summary>
    public void Logout()
    {
        _authState.Logout();
    }

    /// <summary>
    /// Logs out the current user (async version)
    /// </summary>
    public async Task LogoutAsync()
    {
        await _authService.SignOutAsync();
        _authState.Logout();
    }

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public User? GetCurrentUser()
    {
        return _authState.CurrentUser;
    }

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        return _authState.IsAuthenticated;
    }

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return _authState.HasRole(role);
    }

    /// <summary>
    /// Updates user profile
    /// Facade method that coordinates service and state updates
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(User user)
    {
        try
        {
            var updatedUser = await _authService.UpdateUserAsync(user);
            _authState.CurrentUser = updatedUser;

            return (true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Response model for authentication operations
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public User? User { get; set; }
}
