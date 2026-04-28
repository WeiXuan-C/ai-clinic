using AiClinic.Interfaces;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies doctor management operations by coordinating services and state
/// </summary>
public class DoctorProfileController
{
    private readonly DoctorProfileService _doctorProfileService;
    private readonly DoctorProfileState _doctorProfileState;
    private readonly AuthState _authState;

    public DoctorProfileController(
        DoctorProfileService doctorProfileService,
        DoctorProfileState doctorProfileState,
        AuthState authState)
    {
        _doctorProfileService = doctorProfileService;
        _doctorProfileState = doctorProfileState;
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
            _doctorProfileState.IsLoading = true;

            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var doctor = await _doctorProfileService.GetDoctorByUserIdAsync(userId.Value);

            if (doctor == null)
            {
                return (false, "Doctor profile not found");
            }

            _doctorProfileState.CurrentDoctor = doctor as Doctor;

            return (true, "Doctor profile loaded successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _doctorProfileState.IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a new doctor profile
    /// </summary>
    public async Task<(bool Success, string Message, Doctor? Doctor)> CreateDoctorProfileAsync(Guid userId, string fullName, string licenseNumber, string specialization)
    {
        try
        {
            var doctorProfile = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FullName = fullName,
                LicenseNumber = licenseNumber,
                PrimarySpecialization = specialization,
                IsActive = true,
                IsVerified = false,
                IsAcceptingPatients = true,
                AvailabilityStatus = "available",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var doctor = await _doctorProfileService.CreateDoctorProfileAsync(doctorProfile);
            _doctorProfileState.CurrentDoctor = doctor as Doctor;
            return (true, "Doctor profile created successfully", doctor as Doctor);
        }
        catch (Exception ex)
        {
            return (false, $"Error creating profile: {ex.Message}", null);
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
            _doctorProfileState.IsLoading = true;

            var doctors = await _doctorProfileService.GetAvailableDoctorsAsync();
            _doctorProfileState.SetAvailableDoctors(doctors.Cast<Doctor>());

            return (true, $"Loaded {doctors.Count()} available doctors");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _doctorProfileState.IsLoading = false;
        }
    }

    /// <summary>
    /// Searches doctors by specialization
    /// </summary>
    public async Task<IEnumerable<Doctor>> SearchDoctorsBySpecializationAsync(string specialization)
    {
        var doctors = await _doctorProfileService.GetDoctorsBySpecializationAsync(specialization);
        return doctors.Cast<Doctor>();
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

            var doctor = _doctorProfileState.CurrentDoctor;
            if (doctor == null)
            {
                return (false, "Doctor profile not loaded");
            }

            await _doctorProfileService.UpdateDoctorAvailabilityAsync(doctor.Id, status);
            _doctorProfileState.UpdateAvailabilityStatus(doctor.Id, status);

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
        var doctor = _doctorProfileState.CurrentDoctor;
        if (doctor == null)
        {
            return Enumerable.Empty<Conversation>();
        }

        var conversations = await _doctorProfileService.GetDoctorConversationsAsync(doctor.Id);
        return conversations.Cast<Conversation>();
    }

    /// <summary>
    /// Updates doctor profile
    /// Facade method that coordinates profile update and state refresh
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateDoctorProfileAsync(Doctor doctor)
    {
        try
        {
            var updatedDoctor = await _doctorProfileService.UpdateDoctorProfileAsync(doctor);
            _doctorProfileState.CurrentDoctor = updatedDoctor as Doctor;

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
        var doctors = await _doctorProfileService.GetAllDoctorsAsync();
        return doctors.Cast<Doctor>();
    }

    /// <summary>
    /// Gets a specific doctor by ID
    /// </summary>
    public async Task<Doctor?> GetDoctorByIdAsync(Guid doctorId)
    {
        var doctor = await _doctorProfileService.GetDoctorByIdAsync(doctorId);
        return doctor as Doctor;
    }

    /// <summary>
    /// Finds an available doctor by specialization
    /// </summary>
    public async Task<Doctor?> FindAvailableDoctorAsync(string? specialization)
    {
        if (string.IsNullOrEmpty(specialization))
        {
            return null;
        }
        var doctor = await _doctorProfileService.FindAvailableDoctorAsync(specialization);
        return doctor as Doctor;
    }

    /// <summary>
    /// Finds the best available doctor
    /// </summary>
    public async Task<Doctor?> FindBestAvailableDoctorAsync(string? specialization = null)
    {
        var doctor = await _doctorProfileService.FindAvailableDoctorAsync(specialization ?? "general");
        return doctor as Doctor;
    }
}
