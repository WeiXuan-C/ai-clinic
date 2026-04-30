using ai_clinic.Interfaces;
using ai_clinic.UI.State;

namespace ai_clinic.Services;

/// <summary>
/// Authentication Service - Business Logic Layer
/// Coordinates between DAOs, Auth Client, and State
/// </summary>
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ISupabaseAuthClient _supabaseAuth;
    private readonly AuthState _authState;

    public AuthService(
        IUserRepository userRepository,
        ISupabaseAuthClient supabaseAuth,
        AuthState authState)
    {
        _userRepository = userRepository;
        _supabaseAuth = supabaseAuth;
        _authState = authState;
    }

    /// <summary>
    /// Sends OTP to user's email
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            _authState.IsLoading = true;
            _authState.ErrorMessage = null;

            var success = await _supabaseAuth.SendOtpAsync(email);

            if (!success)
            {
                _authState.ErrorMessage = "Failed to send OTP";
            }

            return success;
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Verifies OTP and authenticates user
    /// </summary>
    public async Task<(bool Success, IUser? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            _authState.IsLoading = true;
            _authState.ErrorMessage = null;

            // Verify OTP with Supabase Auth
            var (success, accessToken, refreshToken, error) = await _supabaseAuth.VerifyOtpAsync(email, otp);

            if (!success)
            {
                _authState.ErrorMessage = error;
                return (false, null, error, null, null);
            }

            // Check if user exists in local database
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // New user - OTP verified but not in local DB yet
                Console.WriteLine($"📝 New user verified: {email}");
                return (true, null, null, accessToken, refreshToken);
            }

            // Existing user - update login timestamp
            var updatedUser = user.WithUpdatedLogin();
            await _userRepository.UpdateAsync(updatedUser);

            // Update state
            _authState.SetAuthentication(updatedUser, accessToken, refreshToken);

            Console.WriteLine($"✅ Existing user logged in: {email}");

            return (true, updatedUser, null, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message.ToLower() switch
            {
                var msg when msg.Contains("expired") => "OTP code has expired. Please click 'Resend Code' to get a new one.",
                var msg when msg.Contains("invalid") => "Invalid OTP code. Please check your email and try again.",
                var msg when msg.Contains("too many") => "Too many attempts. Please wait a few minutes and try again.",
                _ => $"Verification failed: {ex.Message}"
            };

            _authState.ErrorMessage = errorMessage;
            return (false, null, errorMessage, null, null);
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        try
        {
            _authState.IsLoading = true;
            _authState.ErrorMessage = null;

            var success = await _supabaseAuth.RestoreSessionAsync(accessToken, refreshToken);

            if (success)
            {
                _authState.AccessToken = accessToken;
                _authState.RefreshToken = refreshToken;
            }

            return success;
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        try
        {
            _authState.IsLoading = true;
            _authState.ErrorMessage = null;

            var (success, accessToken, refreshToken) = await _supabaseAuth.RefreshSessionAsync();

            if (success && accessToken != null)
            {
                _authState.AccessToken = accessToken;
                _authState.RefreshToken = refreshToken;
            }

            return (success, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return (false, null, null);
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            _authState.IsLoading = true;

            await _supabaseAuth.SignOutAsync();

            _authState.Logout();

            Console.WriteLine("✅ User signed out");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error signing out: {ex.Message}");
            _authState.ErrorMessage = ex.Message;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Gets user by ID from repository
    /// </summary>
    public async Task<IUser?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            _authState.IsLoading = true;
            return await _userRepository.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Gets user by email from repository
    /// </summary>
    public async Task<IUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            _authState.IsLoading = true;
            return await _userRepository.GetByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    public async Task<IUser?> UpdateUserAsync(IUser user)
    {
        try
        {
            _authState.IsLoading = true;

            var concreteUser = user as User ?? new User
            {
                Id = user.Id,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                LastLoginAt = user.LastLoginAt,
                DataSharingEnabled = user.DataSharingEnabled,
                AiAnalysisEnabled = user.AiAnalysisEnabled,
                ActivityTrackingEnabled = user.ActivityTrackingEnabled,
                IsDeactivated = user.IsDeactivated,
                DeactivatedAt = user.DeactivatedAt
            };

            var updated = await _userRepository.UpdateAsync(concreteUser);

            // Update state if it's the current user
            if (_authState.CurrentUser?.Id == user.Id)
            {
                _authState.CurrentUser = updated;
            }

            return updated;
        }
        catch (Exception ex)
        {
            _authState.ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new user with role and profile
    /// </summary>
    public async Task<IUser?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        try
        {
            _authState.IsLoading = true;

            email = email.ToLower();

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                Console.WriteLine($"⚠️ User already exists: {email}");
                return existingUser;
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DataSharingEnabled = true,
                AiAnalysisEnabled = true,
                ActivityTrackingEnabled = true
            };

            var createdUser = await _userRepository.AddAsync(user);

            Console.WriteLine($"✅ User created: {email} with role: {role}");

            // Update state
            _authState.CurrentUser = createdUser;

            return createdUser;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating user: {ex.Message}");
            _authState.ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            _authState.IsLoading = false;
        }
    }

    // Convenience methods for accessing state
    public IUser? GetCurrentUser() => _authState.CurrentUser;
    public bool IsAuthenticated() => _authState.IsAuthenticated;
    public string? GetUserRole() => _authState.UserRole;
    public Guid? GetUserId() => _authState.UserId;
    public bool HasRole(string role) => _authState.HasRole(role);
    public bool IsPatient() => _authState.IsPatient;
    public bool IsDoctor() => _authState.IsDoctor;
    public bool IsAdmin() => _authState.IsAdmin;
    public string? GetAccessToken() => _authState.AccessToken;
    public string? GetRefreshToken() => _authState.RefreshToken;
    public bool IsLoading() => _authState.IsLoading;
    public string? GetErrorMessage() => _authState.ErrorMessage;
    public void ClearError() => _authState.ClearError();
}
