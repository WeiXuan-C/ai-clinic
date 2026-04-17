using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly SupabaseContext _context;

    public AuthService(
        IUserRepository userRepository,
        IPatientProfileRepository patientProfileRepository,
        IDoctorRepository doctorRepository,
        SupabaseContext context)
    {
        _userRepository = userRepository;
        _patientProfileRepository = patientProfileRepository;
        _doctorRepository = doctorRepository;
        _context = context;
    }

    public async Task<AuthResponse> SignInWithOtpAsync(string email)
    {
        try
        {
            // Initialize Supabase client if needed
            await _context.Client.InitializeAsync();
            
            // Use SendMagicLink for email OTP
            var result = await _context.Client.Auth.SendMagicLink(email);
            
            return new AuthResponse(true, null, "Verification code sent to your email", null);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ParseSupabaseError(ex.Message);
            return new AuthResponse(false, null, friendlyMessage, null);
        }
    }
    
    public async Task<AuthResponse> SignUpWithOtpAsync(string email)
    {
        try
        {
            // Initialize Supabase client if needed
            await _context.Client.InitializeAsync();
            
            // Use SendMagicLink for email OTP (Supabase handles auto-registration)
            var result = await _context.Client.Auth.SendMagicLink(email);
            
            return new AuthResponse(true, null, "Verification code sent to your email", null);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ParseSupabaseError(ex.Message);
            return new AuthResponse(false, null, friendlyMessage, null);
        }
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string token)
    {
        try
        {
            // Try different OTP types - MagicLink is for links, but when template shows {{ .Token }}
            // we might need to try Signup type
            Supabase.Gotrue.Session? session = null;
            
            try
            {
                // First try with MagicLink type
                session = await _context.Client.Auth.VerifyOTP(email, token, Supabase.Gotrue.Constants.EmailOtpType.MagicLink);
            }
            catch
            {
                // If that fails, try with Signup type (works for both signup and signin with OTP)
                session = await _context.Client.Auth.VerifyOTP(email, token, Supabase.Gotrue.Constants.EmailOtpType.Signup);
            }
            
            if (session?.User == null)
            {
                return new AuthResponse(false, null, "Invalid or expired verification code. Please request a new one.", null);
            }

            // Check if user exists in our database
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user != null)
            {
                // Existing user - update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                
                var userDto = new UserDto(user.Id, user.Email, null, user.Role);
                return new AuthResponse(true, session.AccessToken, "Login successful", userDto);
            }
            
            // New user - DO NOT create user record yet
            // Just verify the OTP and return success without user data
            // User will be created when they complete registration with role and full name
            return new AuthResponse(true, session.AccessToken, "Email verified successfully", null);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ParseSupabaseError(ex.Message);
            return new AuthResponse(false, null, friendlyMessage, null);
        }
    }

    private string ParseSupabaseError(string errorMessage)
    {
        // Parse JSON error from Supabase
        if (errorMessage.Contains("over_email_send_rate_limit"))
        {
            // Extract seconds if available
            var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"after (\d+) seconds");
            if (match.Success)
            {
                var seconds = match.Groups[1].Value;
                return $"Please wait {seconds} seconds before requesting another code. This helps keep your account secure.";
            }
            return "You've requested too many codes. Please wait a moment before trying again.";
        }
        
        if (errorMessage.Contains("429") || errorMessage.Contains("rate_limit"))
        {
            return "Too many requests. Please wait a moment and try again.";
        }
        
        if (errorMessage.Contains("invalid_credentials") || errorMessage.Contains("Invalid login") || errorMessage.Contains("invalid token"))
        {
            return "Invalid verification code. Please check the code and try again.";
        }
        
        if (errorMessage.Contains("Token has expired") || errorMessage.Contains("otp_expired") || errorMessage.Contains("expired"))
        {
            return "Your verification code has expired. Please request a new one.";
        }
        
        if (errorMessage.Contains("email_not_confirmed"))
        {
            return "Please verify your email address first.";
        }
        
        if (errorMessage.Contains("user_not_found"))
        {
            return "No account found with this email address.";
        }
        
        if (errorMessage.Contains("invalid_grant"))
        {
            return "Invalid or expired verification code. Please request a new one.";
        }
        
        if (errorMessage.Contains("smtp") || errorMessage.Contains("email") && errorMessage.Contains("send"))
        {
            return "Unable to send email. Please contact support or try again later.";
        }
        
        if (errorMessage.Contains("400") || errorMessage.Contains("Bad Request"))
        {
            return "Invalid request. Please check your input and try again.";
        }
        
        // Default fallback for unknown errors
        if (errorMessage.Length > 200)
        {
            return "Something went wrong. Please try again or contact support.";
        }
        
        return errorMessage;
    }

    public async Task<AuthResponse> CompleteRegistrationAsync(string email, string fullName, string role)
    {
        try
        {
            // Get the current authenticated user from Supabase
            var supabaseUser = _context.Client.Auth.CurrentUser;
            
            if (supabaseUser == null || supabaseUser.Email != email)
            {
                return new AuthResponse(false, null, "Session expired. Please verify your email again.", null);
            }

            // Check if user already exists in our database
            var existingUser = await _userRepository.GetByEmailAsync(email);
            
            if (existingUser != null)
            {
                return new AuthResponse(false, null, "This account is already registered.", null);
            }

            // Create user in our database
            var user = new User
            {
                Id = Guid.Parse(supabaseUser.Id ?? Guid.NewGuid().ToString()),
                Email = email,
                Role = role.ToLower(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            
            user = await _userRepository.AddAsync(user);

            // Create patient or doctor profile based on role
            if (role.ToLower() == "patient")
            {
                var patientProfile = new PatientProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FullName = fullName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _patientProfileRepository.AddAsync(patientProfile);
            }
            else if (role.ToLower() == "doctor")
            {
                var doctor = new Doctor
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FullName = fullName,
                    LicenseNumber = "", // Will be filled later
                    PrimarySpecialization = "", // Will be filled later
                    AvailabilityStatus = "offline",
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _doctorRepository.AddAsync(doctor);
            }

            return new AuthResponse(true, null, "Registration completed successfully", null);
        }
        catch (Exception ex)
        {
            var friendlyMessage = ParseSupabaseError(ex.Message);
            return new AuthResponse(false, null, friendlyMessage, null);
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

    public async Task<bool> IsSignedInAsync()
    {
        try
        {
            // Get current session
            var session = _context.Client.Auth.CurrentSession;
            
            if (session == null || string.IsNullOrEmpty(session.AccessToken))
            {
                return false;
            }

            // Use Supabase GetUser with JWT to check if the user is authenticated
            var supabaseUser = await _context.Client.Auth.GetUser(session.AccessToken);
            
            // Check if user exists and has a valid session
            return supabaseUser != null && !string.IsNullOrEmpty(supabaseUser.Id);
        }
        catch
        {
            // If GetUser throws an exception, user is not signed in
            return false;
        }
    }

    public async Task<(bool isSignedIn, UserDto? user)> CheckAuthenticationAsync()
    {
        try
        {
            // Get current session
            var session = _context.Client.Auth.CurrentSession;
            
            if (session == null || string.IsNullOrEmpty(session.AccessToken))
            {
                return (false, null);
            }

            // Use Supabase GetUser with JWT to get the current authenticated user
            var supabaseUser = await _context.Client.Auth.GetUser(session.AccessToken);
            
            if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
            {
                return (false, null);
            }

            // Get user from our database
            var user = await _userRepository.GetByEmailAsync(supabaseUser.Email ?? "");
            
            if (user == null)
            {
                // User is authenticated in Supabase but not in our database
                // This means they verified email but didn't complete registration
                return (true, null);
            }

            var userDto = new UserDto(user.Id, user.Email, null, user.Role);
            return (true, userDto);
        }
        catch
        {
            // If GetUser throws an exception, user is not signed in
            return (false, null);
        }
    }

    public string? GetCurrentSessionToken()
    {
        try
        {
            var session = _context.Client.Auth.CurrentSession;
            return session?.AccessToken;
        }
        catch
        {
            return null;
        }
    }
}
