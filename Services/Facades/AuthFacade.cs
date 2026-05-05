using ai_clinic.Models;

namespace ai_clinic.Services.Facades;

/// <summary>
/// Facade pattern implementation for authentication operations
/// Coordinates between UserService, PatientProfileService, DoctorProfileService, and ActivityLogService
/// </summary>
public class AuthFacade
{
    private readonly UserService _userService;
    private readonly PatientProfileService _patientProfileService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly ActivityLogService _activityLogService;
    private readonly AuthStateService _authStateService;

    public AuthFacade(
        UserService userService,
        PatientProfileService patientProfileService,
        DoctorProfileService doctorProfileService,
        ActivityLogService activityLogService,
        AuthStateService authStateService)
    {
        _userService = userService;
        _patientProfileService = patientProfileService;
        _doctorProfileService = doctorProfileService;
        _activityLogService = activityLogService;
        _authStateService = authStateService;
    }

    /// <summary>
    /// Register a new user with the specified role
    /// Creates user account and corresponding profile (Patient or Doctor)
    /// </summary>
    public async Task<AuthResult> RegisterAsync(string email, string password, UserRole role, string? ipAddress = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResult.Failure("Email is required");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.Failure("Password is required");
            }

            if (password.Length < 8)
            {
                return AuthResult.Failure("Password must be at least 8 characters");
            }

            // Check if user already exists
            var existingUser = await _userService.GetByEmailAsync(email);
            if (existingUser != null)
            {
                return AuthResult.Failure("An account with this email already exists");
            }

            // Create user
            var user = new User
            {
                Email = email,
                Role = role,
                IsActive = true
            };

            user = await _userService.CreateAsync(user, password);

            // Create corresponding profile based on role
            if (role == UserRole.Patient)
            {
                var patientProfile = new PatientProfile
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _patientProfileService.CreateAsync(patientProfile);
            }
            else if (role == UserRole.Doctor)
            {
                var doctorProfile = new DoctorProfile
                {
                    UserId = user.Id,
                    FullName = string.Empty, // Will be updated later
                    LicenseNumber = string.Empty, // Will be updated later
                    PrimarySpecialization = string.Empty, // Will be updated later
                    IsActive = false, // Requires verification
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _doctorProfileService.CreateAsync(doctorProfile);
            }

            // Log activity
            await _activityLogService.LogActivityAsync(
                user.Id,
                "user_registered",
                $"New {role} account created",
                ipAddress
            );

            // Set current user in auth state
            await _authStateService.SetCurrentUserAsync(user, _userService, _patientProfileService, _doctorProfileService);

            return AuthResult.Success(user);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Authenticate user with email and password
    /// Updates last login time and logs activity
    /// </summary>
    public async Task<AuthResult> SignInAsync(string email, string password, string? ipAddress = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResult.Failure("Email is required");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.Failure("Password is required");
            }

            // Authenticate user
            var user = await _userService.AuthenticateAsync(email, password);
            if (user == null)
            {
                // Log failed attempt
                await _activityLogService.LogActivityAsync(
                    null,
                    "login_failed",
                    $"Failed login attempt for email: {email}",
                    ipAddress
                );

                return AuthResult.Failure("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive || user.IsDeactivated)
            {
                return AuthResult.Failure("This account has been deactivated");
            }

            // Log successful login
            await _activityLogService.LogActivityAsync(
                user.Id,
                "user_login",
                $"User logged in successfully",
                ipAddress
            );

            // Set current user in auth state
            await _authStateService.SetCurrentUserAsync(user, _userService, _patientProfileService, _doctorProfileService);

            return AuthResult.Success(user);
        }
        catch (Exception ex)
        {
            return AuthResult.Failure($"Sign in failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sign out user and log activity
    /// </summary>
    public async Task SignOutAsync(Guid userId, string? ipAddress = null)
    {
        await _activityLogService.LogActivityAsync(
            userId,
            "user_logout",
            "User logged out",
            ipAddress
        );
        
        await _authStateService.ClearCurrentUserAsync();
    }

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public bool IsAuthenticated => _authStateService.IsAuthenticated;

    /// <summary>
    /// Get current authenticated user
    /// </summary>
    public User? CurrentUser => _authStateService.CurrentUser;

    /// <summary>
    /// Get current user's patient profile (if user is a patient)
    /// </summary>
    public PatientProfile? CurrentPatientProfile => _authStateService.CurrentPatientProfile;

    /// <summary>
    /// Get current user's doctor profile (if user is a doctor)
    /// </summary>
    public DoctorProfile? CurrentDoctorProfile => _authStateService.CurrentDoctorProfile;

    /// <summary>
    /// Check if authentication state is initialized
    /// </summary>
    public bool IsInitialized => _authStateService.IsInitialized;

    /// <summary>
    /// Check if current user is a patient
    /// </summary>
    public bool IsPatient => _authStateService.IsPatient;

    /// <summary>
    /// Check if current user is a doctor
    /// </summary>
    public bool IsDoctor => _authStateService.IsDoctor;

    /// <summary>
    /// Check if current user is an admin
    /// </summary>
    public bool IsAdmin => _authStateService.IsAdmin;

    /// <summary>
    /// Event triggered when authentication state changes
    /// </summary>
    public event Action? OnAuthStateChanged
    {
        add => _authStateService.OnAuthStateChanged += value;
        remove => _authStateService.OnAuthStateChanged -= value;
    }

    /// <summary>
    /// Get user initials for avatar display
    /// </summary>
    public string GetUserInitials()
    {
        return _authStateService.GetUserInitials();
    }

    /// <summary>
    /// Get display name for current user
    /// </summary>
    public string GetDisplayName()
    {
        return _authStateService.GetDisplayName();
    }

    /// <summary>
    /// Notify subscribers that auth state has changed
    /// Call this after updating profile information
    /// </summary>
    public void NotifyStateChanged()
    {
        _authStateService.NotifyStateChanged();
    }

    /// <summary>
    /// Force re-check authentication from cookie
    /// Useful when you suspect the session might have changed
    /// </summary>
    public async Task<bool> RevalidateSessionAsync()
    {
        return await _authStateService.RevalidateSessionAsync(_userService, _patientProfileService, _doctorProfileService);
    }

    /// <summary>
    /// Initialize authentication state from stored session
    /// </summary>
    public async Task InitializeAuthStateAsync()
    {
        if (!_authStateService.IsInitialized)
        {
            await _authStateService.InitializeAsync(_userService, _patientProfileService, _doctorProfileService);
        }
    }

    /// <summary>
    /// Get user with profile information
    /// </summary>
    public async Task<User?> GetUserWithProfileAsync(Guid userId)
    {
        return await _userService.GetByIdAsync(userId);
    }

    /// <summary>
    /// Refresh current user data from database
    /// </summary>
    public async Task RefreshCurrentUserAsync()
    {
        if (_authStateService.CurrentUser != null)
        {
            await _authStateService.SetCurrentUserAsync(
                _authStateService.CurrentUser,
                _userService,
                _patientProfileService,
                _doctorProfileService
            );
        }
    }
}

/// <summary>
/// Result object for authentication operations
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }

    public static AuthResult Success(User user)
    {
        return new AuthResult
        {
            IsSuccess = true,
            User = user
        };
    }

    public static AuthResult Failure(string errorMessage)
    {
        return new AuthResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
