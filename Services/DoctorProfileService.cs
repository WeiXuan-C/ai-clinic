using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Doctor Profile Service - Business Logic Layer
/// Handles doctor profile operations through state management
/// </summary>
public class DoctorProfileService
{
    private readonly DoctorProfileState _state;

    public DoctorProfileService(DoctorProfileState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets a doctor profile by ID
    /// </summary>
    public async Task<Doctor?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets a doctor profile by user ID
    /// </summary>
    public async Task<Doctor?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all doctor profiles
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets all available doctors
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        return await _state.GetAvailableDoctorsAsync();
    }

    /// <summary>
    /// Gets doctors by specialization
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _state.GetBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Gets doctors by organization ID
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetDoctorsByOrganizationAsync(Guid organizationId)
    {
        return await _state.GetByOrganizationIdAsync(organizationId);
    }

    /// <summary>
    /// Updates doctor availability status
    /// </summary>
    public async Task UpdateAvailabilityStatusAsync(Guid doctorId, string status)
    {
        await _state.UpdateAvailabilityStatusAsync(doctorId, status);
    }

    /// <summary>
    /// Creates a new doctor profile
    /// </summary>
    public async Task<Doctor?> CreateProfileAsync(Doctor doctor)
    {
        return await _state.CreateAsync(doctor);
    }

    /// <summary>
    /// Updates a doctor profile
    /// </summary>
    public async Task<Doctor?> UpdateProfileAsync(Doctor doctor)
    {
        return await _state.UpdateAsync(doctor);
    }

    /// <summary>
    /// Deletes a doctor profile
    /// </summary>
    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets the current profile from state
    /// </summary>
    public Doctor? GetCurrentProfile()
    {
        return _state.CurrentProfile;
    }

    /// <summary>
    /// Gets cached doctors from state
    /// </summary>
    public IReadOnlyList<Doctor> GetCachedDoctors()
    {
        return _state.Doctors;
    }

    /// <summary>
    /// Checks if profile exists in state
    /// </summary>
    public bool HasProfile()
    {
        return _state.HasProfile;
    }
}
