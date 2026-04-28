using AiClinic.Interfaces;
using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Authentication Service - Business Logic Layer
/// Handles authentication operations through state management
/// </summary>
public class AuthService
{
    private readonly AuthState _state;

    public AuthService(AuthState state)
    {
        _state = state;
    }

    /// <summary>
    /// Sends OTP to user's email
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        return await _state.SendOtpAsync(email);
    }

    /// <summary>
    /// Verifies OTP and authenticates user
    /// </summary>
    public async Task<(bool Success, IUser? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        return await _state.VerifyOtpAsync(email, otp);
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        return await _state.RestoreSessionAsync(accessToken, refreshToken);
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        return await _state.RefreshSessionAsync();
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public async Task SignOutAsync()
    {
        await _state.SignOutAsync();
    }

    /// <summary>
    /// Gets user by ID
    /// </summary>
    public async Task<IUser?> GetUserByIdAsync(Guid userId)
    {
        return await _state.GetUserByIdAsync(userId);
    }

    /// <summary>
    /// Gets user by email
    /// </summary>
    public async Task<IUser?> GetUserByEmailAsync(string email)
    {
        return await _state.GetUserByEmailAsync(email);
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    public async Task<IUser?> UpdateUserAsync(IUser user)
    {
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
        return await _state.UpdateUserAsync(concreteUser);
    }

    /// <summary>
    /// Creates a new user with role and profile
    /// </summary>
    public async Task<IUser?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        return await _state.CreateUserWithProfileAsync(email, fullName, role);
    }

    /// <summary>
    /// Gets the current authenticated user from state
    /// </summary>
    public IUser? GetCurrentUser()
    {
        return _state.CurrentUser;
    }

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        return _state.IsAuthenticated;
    }

    /// <summary>
    /// Gets current user's role
    /// </summary>
    public string? GetUserRole()
    {
        return _state.UserRole;
    }

    /// <summary>
    /// Gets current user's ID
    /// </summary>
    public Guid? GetUserId()
    {
        return _state.UserId;
    }

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return _state.HasRole(role);
    }

    /// <summary>
    /// Checks if current user is a patient
    /// </summary>
    public bool IsPatient()
    {
        return _state.IsPatient;
    }

    /// <summary>
    /// Checks if current user is a doctor
    /// </summary>
    public bool IsDoctor()
    {
        return _state.IsDoctor;
    }

    /// <summary>
    /// Checks if current user is an admin
    /// </summary>
    public bool IsAdmin()
    {
        return _state.IsAdmin;
    }

    /// <summary>
    /// Gets access token
    /// </summary>
    public string? GetAccessToken()
    {
        return _state.AccessToken;
    }

    /// <summary>
    /// Gets refresh token
    /// </summary>
    public string? GetRefreshToken()
    {
        return _state.RefreshToken;
    }

    /// <summary>
    /// Gets loading state
    /// </summary>
    public bool IsLoading()
    {
        return _state.IsLoading;
    }

    /// <summary>
    /// Gets error message
    /// </summary>
    public string? GetErrorMessage()
    {
        return _state.ErrorMessage;
    }

    /// <summary>
    /// Clears error message
    /// </summary>
    public void ClearError()
    {
        _state.ClearError();
    }
}
