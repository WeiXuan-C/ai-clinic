using ai_clinic.Interfaces;

namespace ai_clinic.UI.State;

/// <summary>
/// Authentication State - State Management Layer
/// Manages authentication state, user data, and calls DAOs for database operations
/// </summary>
public class AuthState
{
    private readonly IUserRepository _userRepository;
    private readonly ISupabaseAuthClient _supabaseAuth;
    private User? _currentUser;
    private bool _isAuthenticated;
    private string? _accessToken;
    private string? _refreshToken;
    private bool _isLoading;
    private string? _errorMessage;
    private readonly Dictionary<string, User> _userCache = new();

    public AuthState(
        IUserRepository userRepository,
        ISupabaseAuthClient supabaseAuth)
    {
        _userRepository = userRepository;
        _supabaseAuth = supabaseAuth;
    }

    /// <summary>
    /// Event triggered when authentication state changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Currently authenticated user
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            _isAuthenticated = value != null;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Access token for Supabase authentication
    /// </summary>
    public string? AccessToken
    {
        get => _accessToken;
        set
        {
            _accessToken = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Refresh token for Supabase authentication
    /// </summary>
    public string? RefreshToken
    {
        get => _refreshToken;
        set
        {
            _refreshToken = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Loading state indicator
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Error message if any operation fails
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Current user's role
    /// </summary>
    public string? UserRole => _currentUser?.Role;

    /// <summary>
    /// Current user's ID
    /// </summary>
    public Guid? UserId => _currentUser?.Id;

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return _currentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if current user is a patient
    /// </summary>
    public bool IsPatient => HasRole("patient");

    /// <summary>
    /// Checks if current user is a doctor
    /// </summary>
    public bool IsDoctor => HasRole("doctor");

    /// <summary>
    /// Checks if current user is an admin
    /// </summary>
    public bool IsAdmin => HasRole("admin");

    /// <summary>
    /// Sends OTP to user's email
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var success = await _supabaseAuth.SendOtpAsync(email);

            if (!success)
            {
                ErrorMessage = "Failed to send OTP";
            }

            return success;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Verifies OTP and authenticates user
    /// </summary>
    public async Task<(bool Success, IUser? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Verify OTP with Supabase Auth
            var (success, accessToken, refreshToken, error) = await _supabaseAuth.VerifyOtpAsync(email, otp);

            if (!success)
            {
                ErrorMessage = error;
                return (false, null, error, null, null);
            }

            // Check if user exists in local database
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // New user - OTP verified but not in local DB yet
                Console.WriteLine($"📝 New user verified: {email}");
                
                // Store tokens
                _accessToken = accessToken;
                _refreshToken = refreshToken;
                
                return (true, null, null, accessToken, refreshToken);
            }

            // Existing user - update login timestamp
            var updatedUser = user.WithUpdatedLogin();
            await _userRepository.UpdateAsync(updatedUser);

            // Update state
            _currentUser = updatedUser;
            _isAuthenticated = true;
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _userCache[email.ToLower()] = updatedUser;

            Console.WriteLine($"✅ Existing user logged in: {email}");

            NotifyStateChanged();

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

            ErrorMessage = errorMessage;
            return (false, null, errorMessage, null, null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var success = await _supabaseAuth.RestoreSessionAsync(accessToken, refreshToken);

            if (success)
            {
                _accessToken = accessToken;
                _refreshToken = refreshToken;
            }

            return success;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var (success, accessToken, refreshToken) = await _supabaseAuth.RefreshSessionAsync();

            if (success && accessToken != null)
            {
                _accessToken = accessToken;
                _refreshToken = refreshToken;
            }

            return (success, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return (false, null, null);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            IsLoading = true;

            await _supabaseAuth.SignOutAsync();

            // Clear state
            _currentUser = null;
            _isAuthenticated = false;
            _accessToken = null;
            _refreshToken = null;
            _userCache.Clear();

            Console.WriteLine("✅ User signed out");

            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error signing out: {ex.Message}");
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets user by ID from repository
    /// </summary>
    public async Task<IUser?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            IsLoading = true;
            return await _userRepository.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets user by email from repository
    /// </summary>
    public async Task<IUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            IsLoading = true;
            
            // Check cache first
            var emailKey = email.ToLower();
            if (_userCache.TryGetValue(emailKey, out var cached))
            {
                return cached;
            }

            // Get from database
            var user = await _userRepository.GetByEmailAsync(email);
            
            // Update cache
            if (user != null)
            {
                _userCache[emailKey] = user;
            }
            
            return user;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    public async Task<IUser?> UpdateUserAsync(IUser user)
    {
        try
        {
            IsLoading = true;

            var concreteUser = user as User ?? new User
            {
                Id = user.Id,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = user.LastLoginAt,
                DataSharingEnabled = user.DataSharingEnabled,
                AiAnalysisEnabled = user.AiAnalysisEnabled,
                ActivityTrackingEnabled = user.ActivityTrackingEnabled,
                IsDeactivated = user.IsDeactivated,
                DeactivatedAt = user.DeactivatedAt
            };

            var updated = await _userRepository.UpdateAsync(concreteUser);

            // Update cache and current user if applicable
            _userCache[updated.Email.ToLower()] = updated;
            if (_currentUser?.Id == user.Id)
            {
                _currentUser = updated;
                NotifyStateChanged();
            }

            return updated;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new user with role and profile
    /// </summary>
    public async Task<IUser?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        try
        {
            IsLoading = true;

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
            _currentUser = createdUser;
            _userCache[email] = createdUser;

            NotifyStateChanged();

            return createdUser;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating user: {ex.Message}");
            ErrorMessage = ex.Message;
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        try
        {
            IsLoading = true;
            return await _userRepository.GetAllAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Enumerable.Empty<User>();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Checks if user exists by email
    /// </summary>
    public async Task<bool> UserExistsAsync(string email)
    {
        try
        {
            return await _userRepository.ExistsAsync(email);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Clears the user cache
    /// </summary>
    public void ClearCache()
    {
        _userCache.Clear();
    }

    /// <summary>
    /// Clears error message
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
