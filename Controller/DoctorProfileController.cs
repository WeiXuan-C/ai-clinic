using ai_clinic.Interfaces;
using ai_clinic.Services;

namespace ai_clinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies doctor management operations by delegating to DoctorProfileService
/// </summary>
public class DoctorProfileController(DoctorProfileService doctorProfileService)
{
    /// <summary>
    /// Loads current doctor's profile
    /// </summary>
    public async Task<(bool Success, string Message)> LoadDoctorProfileAsync()
    {
        try
        {
            await doctorProfileService.LoadCurrentDoctorProfileAsync();
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
        Console.WriteLine($"🎯 DoctorProfileController.CreateDoctorProfileAsync called");
        Console.WriteLine($"   UserId: {userId}");
        Console.WriteLine($"   FullName: {fullName}");
        Console.WriteLine($"   LicenseNumber: {licenseNumber}");
        Console.WriteLine($"   Specialization: {specialization}");
        
        try
        {
            Console.WriteLine($"📞 Calling doctorProfileService.CreateDoctorProfileAsync...");
            var profile = await doctorProfileService.CreateDoctorProfileAsync(userId, fullName, licenseNumber, specialization);
            
            if (profile == null)
            {
                Console.WriteLine($"❌ Profile creation returned null");
                return (false, "Failed to create doctor profile");
            }
            
            Console.WriteLine($"✅ Profile created successfully: {profile.Id}");
            return (true, "Doctor profile created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception in CreateDoctorProfileAsync: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
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
            var doctors = await doctorProfileService.GetAvailableDoctorsAsync();
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
    public Task<IEnumerable<IDoctorProfile>> SearchDoctorsBySpecializationAsync(string specialization)
    {
        return doctorProfileService.GetDoctorsBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Updates doctor availability status
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAvailabilityAsync(string status)
    {
        try
        {
            await doctorProfileService.UpdateCurrentDoctorAvailabilityAsync(status);
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
    public Task<IEnumerable<IConversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return doctorProfileService.GetDoctorConversationsAsync(doctorId);
    }

    /// <summary>
    /// Updates doctor profile
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateDoctorProfileAsync(IDoctorProfile doctor)
    {
        try
        {
            await doctorProfileService.UpdateDoctorProfileAsync(doctor);
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
    public Task<IEnumerable<IDoctorProfile>> GetAllDoctorsAsync()
    {
        return doctorProfileService.GetAllDoctorsAsync();
    }

    /// <summary>
    /// Gets a specific doctor by ID
    /// </summary>
    public Task<IDoctorProfile?> GetDoctorByIdAsync(Guid doctorId)
    {
        return doctorProfileService.GetDoctorByIdAsync(doctorId);
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
        return await doctorProfileService.FindAvailableDoctorAsync(specialization);
    }

    /// <summary>
    /// Finds the best available doctor
    /// </summary>
    public Task<IDoctorProfile?> FindBestAvailableDoctorAsync(string? specialization = null)
    {
        return doctorProfileService.FindAvailableDoctorAsync(specialization ?? "general");
    }
}
