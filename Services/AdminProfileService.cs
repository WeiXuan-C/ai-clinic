using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Admin Profile Service - Business Logic Layer
/// Handles admin profile operations through state management
/// </summary>
public class AdminProfileService
{
    private readonly AdminProfileState _state;

    public AdminProfileService(AdminProfileState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets an admin profile by ID
    /// </summary>
    public async Task<AdminProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets an admin profile by user ID
    /// </summary>
    public async Task<AdminProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all admin profiles
    /// </summary>
    public async Task<IEnumerable<AdminProfile>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Creates a new admin profile
    /// </summary>
    public async Task<AdminProfile?> CreateProfileAsync(AdminProfile profile)
    {
        return await _state.CreateAsync(profile);
    }

    /// <summary>
    /// Updates an admin profile
    /// </summary>
    public async Task<AdminProfile?> UpdateProfileAsync(AdminProfile profile)
    {
        return await _state.UpdateAsync(profile);
    }

    /// <summary>
    /// Deletes an admin profile
    /// </summary>
    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets the current profile from state
    /// </summary>
    public AdminProfile? GetCurrentProfile()
    {
        return _state.CurrentProfile;
    }

    /// <summary>
    /// Checks if profile exists in state
    /// </summary>
    public bool HasProfile()
    {
        return _state.HasProfile;
    }
}
