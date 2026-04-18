using AiClinic.Core.Entities;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies doctor management operations by coordinating services and state
/// </summary>
public class DoctorController
{
    private readonly DoctorService _doctorService;
    private readonly DoctorState _doctorState;
    private readonly AuthState _authState;

    public DoctorController(
        DoctorService doctorService,
        DoctorState doctorState,
        AuthState authState)
    {
        _doctorService = doctorService;
        _doctorState = doctorState;
        _authState = authState;
    }

    /// <summary>
    /// Loads current doctor's profile
    /// Facade method that coordinates loading doctor data and updating state
    /// </summary>
    public async Task<(bool Success, string Message)> LoadDoctorProfileAsync()
    {
        try
        {
            _doctorState.IsLoading = true;

            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var doctor = await _doctorService.GetDoctorByUserIdAsync(userId.Value);

            if (doctor == null)
            {
                return (false, "Doctor profile not found");
            }

            _doctorState.CurrentDoctor = doctor;

            return (true, "Doctor profile loaded successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _doctorState.IsLoading = false;
        }
    }

    /// <summary>
    /// Loads all available doctors
    /// Facade method that updates the available doctors list in state
    /// </summary>
    public async Task<(bool Success, string Message)> LoadAvailableDoctorsAsync()
    {
        try
        {
            _doctorState.IsLoading = true;

            var doctors = await _doctorService.GetAvailableDoctorsAsync();
            _doctorState.SetAvailableDoctors(doctors);

            return (true, $"Loaded {doctors.Count()} available doctors");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _doctorState.IsLoading = false;
        }
    }

    /// <summary>
    /// Searches doctors by specialization
    /// </summary>
    public async Task<IEnumerable<Doctor>> SearchDoctorsBySpecializationAsync(string specialization)
    {
        return await _doctorService.GetDoctorsBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Updates doctor availability status
    /// Facade method that coordinates service update and state update
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAvailabilityAsync(string status)
    {
        try
        {
            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var doctor = _doctorState.CurrentDoctor;
            if (doctor == null)
            {
                return (false, "Doctor profile not loaded");
            }

            await _doctorService.UpdateDoctorAvailabilityAsync(doctor.Id, status);
            _doctorState.UpdateAvailabilityStatus(status);

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
    public async Task<IEnumerable<Conversation>> GetDoctorConversationsAsync()
    {
        var doctor = _doctorState.CurrentDoctor;
        if (doctor == null)
        {
            return Enumerable.Empty<Conversation>();
        }

        return await _doctorService.GetDoctorConversationsAsync(doctor.Id);
    }

    /// <summary>
    /// Updates doctor profile
    /// Facade method that coordinates profile update and state refresh
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateDoctorProfileAsync(Doctor doctor)
    {
        try
        {
            var updatedDoctor = await _doctorService.UpdateDoctorProfileAsync(doctor);
            _doctorState.CurrentDoctor = updatedDoctor;

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
    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
    {
        return await _doctorService.GetAllDoctorsAsync();
    }

    /// <summary>
    /// Gets a specific doctor by ID
    /// </summary>
    public async Task<Doctor?> GetDoctorByIdAsync(Guid doctorId)
    {
        return await _doctorService.GetDoctorByIdAsync(doctorId);
    }

    /// <summary>
    /// Finds the best available doctor
    /// </summary>
    public async Task<Doctor?> FindBestAvailableDoctorAsync(string? specialization = null)
    {
        return await _doctorService.FindAvailableDoctorAsync(specialization);
    }
}
