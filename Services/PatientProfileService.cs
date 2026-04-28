using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Patient Profile Service - Business Logic Layer
/// Handles patient profile operations through state management
/// </summary>
public class PatientProfileService
{
    private readonly PatientProfileState _state;

    public PatientProfileService(PatientProfileState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets a patient profile by ID
    /// </summary>
    public async Task<PatientProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets a patient profile by user ID
    /// </summary>
    public async Task<PatientProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all patient profiles
    /// </summary>
    public async Task<IEnumerable<PatientProfile>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<PatientProfile?> CreateProfileAsync(PatientProfile profile)
    {
        return await _state.CreateAsync(profile);
    }

    /// <summary>
    /// Creates a new patient profile with user ID and name
    /// </summary>
    public async Task<PatientProfile?> CreateProfileAsync(Guid userId, string fullName)
    {
        var profile = PatientProfile.Create(userId, fullName);
        return await _state.CreateAsync(profile);
    }

    /// <summary>
    /// Updates a patient profile
    /// </summary>
    public async Task<PatientProfile?> UpdateProfileAsync(PatientProfile profile)
    {
        return await _state.UpdateAsync(profile);
    }

    /// <summary>
    /// Deletes a patient profile
    /// </summary>
    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets the current profile from state
    /// </summary>
    public PatientProfile? GetCurrentProfile()
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
