using ai_clinic.Interfaces;
using ai_clinic.UI.State;

namespace ai_clinic.Services;

/// <summary>
/// Authentication Service - Business Logic Layer
/// Handles authentication operations through state management
/// </summary>
public class AuthService
{
    private readonly AuthState _authState;

    public AuthService(AuthState authState)
    {
        _authState = authState;
    }

    /// <summary>
    /// Sends OTP to user's email
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        return await _authState.SendOtpAsync(email);
    }

    /// <summary>
    /// Verifies OTP and authenticates user
    /// </summary>
    public async Task<(bool Success, IUser? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        return await _authState.VerifyOtpAsync(email, otp);
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        return await _authState.RestoreSessionAsync(accessToken, refreshToken);
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        return await _authState.RefreshSessionAsync();
    }

    /// <summary>
    /// Signs out the current user
    /// </summary>
    public async Task SignOutAsync()
    {
        await _authState.SignOutAsync();
    }

    /// <summary>
    /// Gets user by ID from repository
    /// </summary>
    public async Task<IUser?> GetUserByIdAsync(Guid userId)
    {
        return await _authState.GetUserByIdAsync(userId);
    }

    /// <summary>
    /// Gets user by email from repository
    /// </summary>
    public async Task<IUser?> GetUserByEmailAsync(string email)
    {
        return await _authState.GetUserByEmailAsync(email);
    }

    /// <summary>
    /// Updates user profile
    /// </summary>
    public async Task<IUser?> UpdateUserAsync(IUser user)
    {
        return await _authState.UpdateUserAsync(user);
    }

    /// <summary>
    /// Creates a new user with role and profile
    /// </summary>
    public async Task<IUser?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        return await _authState.CreateUserWithProfileAsync(email, fullName, role);
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _authState.GetAllUsersAsync();
    }

    /// <summary>
    /// Checks if user exists by email
    /// </summary>
    public async Task<bool> UserExistsAsync(string email)
    {
        return await _authState.UserExistsAsync(email);
    }

    /// <summary>
    /// Clears the user cache
    /// </summary>
    public void ClearCache()
    {
        _authState.ClearCache();
    }

    // Convenience methods for accessing current state
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
    public void ClearError() => _authState.ClearError();
}
