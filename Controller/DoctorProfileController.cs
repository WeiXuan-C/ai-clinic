using AiClinic.Interfaces;
using AiClinic.Services;

namespace AiClinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies doctor management operations by delegating to DoctorProfileService
/// </summary>
public class DoctorProfileController
{
    private readonly DoctorProfileService _doctorProfileService;

    public DoctorProfileController(DoctorProfileService doctorProfileService)
    {
        _doctorProfileService = doctorProfileService;
    }

    /// <summary>
    /// Loads current doctor's profile
    /// </summary>
    public async Task<(bool Success, string Message)> LoadDoctorProfileAsync()
    {
        try
        {
            await _doctorProfileService.LoadCurrentDoctorProfileAsync();
            return (true, "Doctor profile loaded successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new doctor profile
    /// </summary>
    public async Task<(bool Success, string Message)> CreateDoctorProfileAsync(Guid userId, string fullName, string licenseNumber, string specialization)
    {
        try
        {
            await _doctorProfileService.CreateDoctorProfileAsync(userId, fullName, licenseNumber, specialization);
            return (true, "Doctor profile created successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error creating profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads all available doctors
    /// </summary>
    public async Task<(bool Success, string Message)> LoadAvailableDoctorsAsync()
    {
        try
        {
            var doctors = await _doctorProfileService.GetAvailableDoctorsAsync();
            return (true, $"Loaded {doctors.Count()} available doctors");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches doctors by specialization
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> SearchDoctorsBySpecializationAsync(string specialization)
    {
        return await _doctorProfileService.GetDoctorsBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Updates doctor availability status
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAvailabilityAsync(string status)
    {
        try
        {
            await _doctorProfileService.UpdateCurrentDoctorAvailabilityAsync(status);
            return (true, $"Availability updated to {status}");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets doctor's active conversations
    /// </summary>
    public async Task<IEnumerable<IConversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return await _doctorProfileService.GetDoctorConversationsAsync(doctorId);
    }

    /// <summary>
    /// Updates doctor profile
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateDoctorProfileAsync(IDoctorProfile doctor)
    {
        try
        {
            await _doctorProfileService.UpdateDoctorProfileAsync(doctor);
            return (true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all doctors (for listing/admin purposes)
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> GetAllDoctorsAsync()
    {
        return await _doctorProfileService.GetAllDoctorsAsync();
    }

    /// <summary>
    /// Gets a specific doctor by ID
    /// </summary>
    public async Task<IDoctorProfile?> GetDoctorByIdAsync(Guid doctorId)
    {
        return await _doctorProfileService.GetDoctorByIdAsync(doctorId);
    }

    /// <summary>
    /// Finds an available doctor by specialization
    /// </summary>
    public async Task<IDoctorProfile?> FindAvailableDoctorAsync(string? specialization)
    {
        if (string.IsNullOrEmpty(specialization))
        {
            return null;
        }
        return await _doctorProfileService.FindAvailableDoctorAsync(specialization);
    }

    /// <summary>
    /// Finds the best available doctor
    /// </summary>
    public async Task<IDoctorProfile?> FindBestAvailableDoctorAsync(string? specialization = null)
    {
        return await _doctorProfileService.FindAvailableDoctorAsync(specialization ?? "general");
    }
}
