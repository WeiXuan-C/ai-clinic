using AiClinic.Interfaces;
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
    public async Task<IDoctorProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets a doctor profile by user ID
    /// </summary>
    public async Task<IDoctorProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all doctor profiles
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets all available doctors
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> GetAvailableDoctorsAsync()
    {
        return await _state.GetAvailableDoctorsAsync();
    }

    /// <summary>
    /// Gets doctors by specialization
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _state.GetBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Gets doctors by organization ID
    /// </summary>
    public async Task<IEnumerable<IDoctorProfile>> GetDoctorsByOrganizationAsync(Guid organizationId)
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
    public async Task<IDoctorProfile?> CreateProfileAsync(IDoctorProfile doctor)
    {
        var concreteDoctor = doctor as Doctor ?? new Doctor
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            FullName = doctor.FullName,
            Title = doctor.Title,
            LicenseNumber = doctor.LicenseNumber,
            PrimarySpecialization = doctor.PrimarySpecialization,
            SubSpecializations = doctor.SubSpecializations,
            MedicalExpertiseTags = doctor.MedicalExpertiseTags,
            SymptomsExpertise = doctor.SymptomsExpertise,
            ConditionsTreated = doctor.ConditionsTreated,
            ProceduresPerformed = doctor.ProceduresPerformed,
            AgeGroupsTreated = doctor.AgeGroupsTreated,
            LanguagesSpoken = doctor.LanguagesSpoken,
            YearsOfExperience = doctor.YearsOfExperience,
            AvailabilityStatus = doctor.AvailabilityStatus,
            WorkingHours = doctor.WorkingHours,
            CurrentActiveConversations = doctor.CurrentActiveConversations,
            TotalConsultations = doctor.TotalConsultations,
            AverageRating = doctor.AverageRating,
            TotalRatings = doctor.TotalRatings,
            ProfilePhotoUrl = doctor.ProfilePhotoUrl,
            IsVerified = doctor.IsVerified,
            IsActive = doctor.IsActive,
            IsAcceptingPatients = doctor.IsAcceptingPatients,
            CreatedAt = doctor.CreatedAt,
            UpdatedAt = doctor.UpdatedAt
        };
        return await _state.CreateAsync(concreteDoctor);
    }

    /// <summary>
    /// Updates a doctor profile
    /// </summary>
    public async Task<IDoctorProfile?> UpdateProfileAsync(IDoctorProfile doctor)
    {
        var concreteDoctor = doctor as Doctor ?? new Doctor
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            FullName = doctor.FullName,
            Title = doctor.Title,
            LicenseNumber = doctor.LicenseNumber,
            PrimarySpecialization = doctor.PrimarySpecialization,
            SubSpecializations = doctor.SubSpecializations,
            MedicalExpertiseTags = doctor.MedicalExpertiseTags,
            SymptomsExpertise = doctor.SymptomsExpertise,
            ConditionsTreated = doctor.ConditionsTreated,
            ProceduresPerformed = doctor.ProceduresPerformed,
            AgeGroupsTreated = doctor.AgeGroupsTreated,
            LanguagesSpoken = doctor.LanguagesSpoken,
            YearsOfExperience = doctor.YearsOfExperience,
            AvailabilityStatus = doctor.AvailabilityStatus,
            WorkingHours = doctor.WorkingHours,
            CurrentActiveConversations = doctor.CurrentActiveConversations,
            TotalConsultations = doctor.TotalConsultations,
            AverageRating = doctor.AverageRating,
            TotalRatings = doctor.TotalRatings,
            ProfilePhotoUrl = doctor.ProfilePhotoUrl,
            IsVerified = doctor.IsVerified,
            IsActive = doctor.IsActive,
            IsAcceptingPatients = doctor.IsAcceptingPatients,
            CreatedAt = doctor.CreatedAt,
            UpdatedAt = doctor.UpdatedAt
        };
        return await _state.UpdateAsync(concreteDoctor);
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
    public IDoctorProfile? GetCurrentProfile()
    {
        return _state.CurrentProfile;
    }

    /// <summary>
    /// Gets cached doctors from state
    /// </summary>
    public IReadOnlyList<IDoctorProfile> GetCachedDoctors()
    {
        return _state.Doctors.Cast<IDoctorProfile>().ToList();
    }

    /// <summary>
    /// Checks if profile exists in state
    /// </summary>
    public bool HasProfile()
    {
        return _state.HasProfile;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IDoctorProfile?> GetDoctorByUserIdAsync(Guid userId)
    {
        return await GetProfileByUserIdAsync(userId);
    }
    
    public async Task<IDoctorProfile?> CreateDoctorProfileAsync(IDoctorProfile doctor)
    {
        return await CreateProfileAsync(doctor);
    }
    
    public async Task<IDoctorProfile?> UpdateDoctorProfileAsync(IDoctorProfile doctor)
    {
        return await UpdateProfileAsync(doctor);
    }
    
    public async Task<IDoctorProfile?> UpdateDoctorAvailabilityAsync(Guid doctorId, string status)
    {
        await UpdateAvailabilityStatusAsync(doctorId, status);
        return await GetProfileByIdAsync(doctorId);
    }
    
    public async Task<IEnumerable<IConversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        // This should delegate to ConversationService
        // For now, return empty list as placeholder
        return Enumerable.Empty<IConversation>();
    }
    
    public async Task<IEnumerable<IDoctorProfile>> GetAllDoctorsAsync()
    {
        return await GetAllProfilesAsync();
    }
    
    public async Task<IDoctorProfile?> GetDoctorByIdAsync(Guid doctorId)
    {
        return await GetProfileByIdAsync(doctorId);
    }
    
    public async Task<IDoctorProfile?> FindAvailableDoctorAsync(string specialization)
    {
        var doctors = await GetDoctorsBySpecializationAsync(specialization);
        return doctors.FirstOrDefault(d => d.IsAcceptingPatients && d.AvailabilityStatus == "available");
    }
}
