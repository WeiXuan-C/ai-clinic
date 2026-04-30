using ai_clinic.Interfaces;
using ai_clinic.Services;

namespace ai_clinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies authentication flows by delegating to AuthService
/// </summary>
public class AuthController(AuthService authService)
{
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
            Console.WriteLine("🔍 SIGNIN: Checking if {0} exists in local users table...", email);
            var userExists = await authService.GetUserByEmailAsync(email);

            if (userExists == null)
            {
                Console.WriteLine("❌ SIGNIN: User {0} NOT found in local database. Blocking signin.", email);
                return (false, "This email is not registered. Please sign up first.");
            }

            Console.WriteLine("✅ SIGNIN: User {0} found in local database. Proceeding with signin.", email);

            // Send OTP - Supabase Auth user may or may not exist, but we don't care
            var success = await authService.SendOtpAsync(email);

            if (success)
            {
                return (true, "Verification code sent to your email. Please check your inbox.");
            }

            return (false, "Failed to send verification code. Please try again.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ SIGNIN ERROR: {0}", ex.Message);
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Completes login by verifying OTP
    /// Facade method that delegates to AuthService
    /// </summary>
    public async Task<(bool Success, string Message)> CompleteLoginAsync(string email, string otp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return (false, "Email and OTP are required");
            }

            var (success, user, error, accessToken, refreshToken) = await authService.VerifyOtpAsync(email, otp);

            if (!success || user == null)
            {
                return (false, error ?? "Invalid OTP");
            }

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
            Console.WriteLine("🔍 SIGNUP: Checking if {0} exists in local users table...", email);
            var existingUser = await authService.GetUserByEmailAsync(email);

            if (existingUser != null)
            {
                Console.WriteLine("❌ SIGNUP: User {0} already exists in local database. Blocking signup.", email);
                return new AuthResponse { Success = false, Message = "This email is already registered. Please sign in instead." };
            }

            Console.WriteLine("✅ SIGNUP: User {0} NOT found in local database. Proceeding with signup.", email);

            // Send OTP - will create Supabase Auth user if doesn't exist
            var success = await authService.SendOtpAsync(email);

            if (success)
            {
                return new AuthResponse { Success = true, Message = "Verification code sent to your email. Please check your inbox." };
            }

            return new AuthResponse { Success = false, Message = "Failed to send verification code. Please try again." };
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ SIGNUP ERROR: {0}", ex.Message);
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

            Console.WriteLine("🔍 VERIFY OTP: Starting verification for {0}", email);
            var (success, user, error, accessToken, refreshToken) = await authService.VerifyOtpAsync(email, otp);

            if (!success)
            {
                Console.WriteLine("❌ VERIFY OTP: Verification failed - {0}", error);
                return new AuthResponse { Success = false, Message = error ?? "Invalid OTP" };
            }

            Console.WriteLine("✅ VERIFY OTP: Verification successful. User in local DB: {0}", user != null ? "Yes" : "No");

            // For signup: user will be null (new user, not in local DB yet)
            // For signin: user will be populated (existing user in local DB)
            if (user != null)
            {
                // Existing user trying to signup - should have been blocked earlier
                Console.WriteLine("⚠️ VERIFY OTP: User already exists in local DB. This is a signin, not signup.");
                return new AuthResponse { Success = true, Message = "OTP verified successfully", User = user };
            }

            // New user - OTP verified, proceed to role selection
            Console.WriteLine("✅ VERIFY OTP: New user verified. Proceeding to role selection.");
            return new AuthResponse { Success = true, Message = "OTP verified successfully", User = null };
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ VERIFY OTP ERROR: {0}", ex.Message);
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
            var existingUser = await authService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResponse { Success = false, Message = "This account is already registered. Please sign in instead." };
            }

            // Create user record with role
            var user = await authService.CreateUserWithProfileAsync(email, fullName, role);

            if (user == null)
            {
                return new AuthResponse { Success = false, Message = "Failed to create user account" };
            }

            return new AuthResponse { Success = true, Message = "Registration completed successfully", User = user };
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    public void Logout()
    {
        authService.ClearError();
    }

    /// <summary>
    /// Logs out the current user (async version)
    /// </summary>
    public Task LogoutAsync()
    {
        return authService.SignOutAsync();
    }

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public IUser? GetCurrentUser()
    {
        return authService.GetCurrentUser();
    }

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        return authService.IsAuthenticated();
    }

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return authService.HasRole(role);
    }

    /// <summary>
    /// Updates user profile
    /// Facade method that delegates to AuthService
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(IUser user)
    {
        try
        {
            var updatedUser = await authService.UpdateUserAsync(user);

            if (updatedUser == null)
            {
                return (false, "Failed to update profile");
            }

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
    private static bool IsValidEmail(string email)
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
    public IUser? User { get; set; }
}
