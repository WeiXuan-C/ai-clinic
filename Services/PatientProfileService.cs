using AiClinic.Interfaces;
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
    public async Task<IPatientProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets a patient profile by user ID
    /// </summary>
    public async Task<IPatientProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all patient profiles
    /// </summary>
    public async Task<IEnumerable<IPatientProfile>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<IPatientProfile?> CreateProfileAsync(IPatientProfile profile)
    {
        // Convert interface to concrete class if needed
        var concreteProfile = profile as PatientProfile ?? new PatientProfile
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            Address = profile.Address,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            BloodType = profile.BloodType,
            Allergies = profile.Allergies,
            ChronicConditions = profile.ChronicConditions,
            CurrentMedications = profile.CurrentMedications,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
        return await _state.CreateAsync(concreteProfile);
    }

    /// <summary>
    /// Creates a new patient profile with user ID and name
    /// </summary>
    public async Task<IPatientProfile?> CreateProfileAsync(Guid userId, string fullName)
    {
        var profile = new PatientProfile { UserId = userId, FullName = fullName, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        return await _state.CreateAsync(profile);
    }

    /// <summary>
    /// Updates a patient profile
    /// </summary>
    public async Task<IPatientProfile?> UpdateProfileAsync(IPatientProfile profile)
    {
        // Convert interface to concrete class if needed
        var concreteProfile = profile as PatientProfile ?? new PatientProfile
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            Address = profile.Address,
            EmergencyContactName = profile.EmergencyContactName,
            EmergencyContactPhone = profile.EmergencyContactPhone,
            BloodType = profile.BloodType,
            Allergies = profile.Allergies,
            ChronicConditions = profile.ChronicConditions,
            CurrentMedications = profile.CurrentMedications,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
        return await _state.UpdateAsync(concreteProfile);
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

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IPatientProfile?> GetProfileAsync(Guid userId)
    {
        return await GetProfileByUserIdAsync(userId);
    }
    
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        // This would need to check the User table, not PatientProfile
        // For now, return false as a placeholder
        return false;
    }
    
    public async Task<object?> GetUserSettingsAsync(Guid userId)
    {
        // Return user settings - placeholder implementation
        return new
        {
            UserId = userId,
            DataSharingEnabled = true,
            AiAnalysisEnabled = true,
            ActivityTrackingEnabled = true
        };
    }
    
    public async Task<object?> UpdateUserSettingsAsync(Guid userId, object settings)
    {
        // Update user settings - placeholder implementation
        return settings;
    }
    
    public async Task<ISupportTicket?> CreateSupportTicketAsync(ISupportTicket ticket)
    {
        // This should delegate to SupportTicketService
        // For now, return null as placeholder
        return null;
    }
    
    public async Task<IEnumerable<ISupportTicket>> GetUserSupportTicketsAsync(Guid userId)
    {
        // This should delegate to SupportTicketService
        // For now, return empty list as placeholder
        return Enumerable.Empty<ISupportTicket>();
    }
    
    public async Task<ISupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        // This should delegate to SupportTicketService
        // For now, return null as placeholder
        return null;
    }
}
