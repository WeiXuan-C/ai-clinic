using ai_clinic.Models;

namespace ai_clinic.Services;

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
    public bool IsAuthenticated()
    {
        return _authStateService.IsAuthenticated;
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
