using ai_clinic.Interfaces;
using ai_clinic.Services;

namespace ai_clinic.Controller;

/// <summary>
/// Patient Controller - Facade Pattern
/// Simplifies patient profile operations by delegating to PatientProfileService
/// </summary>
public class PatientProfileController(PatientProfileService patientProfileService)
{
    /// <summary>
    /// Gets patient profile
    /// </summary>
    public Task<IPatientProfile?> GetProfileAsync(Guid userId)
    {
        return patientProfileService.GetProfileAsync(userId);
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<(bool Success, string Message)> CreateProfileAsync(Guid userId, string fullName)
    {
        try
        {
            var profile = await patientProfileService.CreateProfileAsync(userId, fullName);

            if (profile == null)
            {
                return (false, "Failed to create patient profile");
            }

            return (true, "Patient profile created successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error creating profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates patient profile
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request)
    {
        try
        {
            await patientProfileService.UpdateProfileAsync(userId, request);
            return (true, "Profile updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error updating profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if email exists
    /// </summary>
    public Task<bool> CheckEmailExistsAsync(string email)
    {
        return patientProfileService.CheckEmailExistsAsync(email);
    }

    /// <summary>
    /// Gets user settings
    /// </summary>
    public Task<IUser?> GetUserSettingsAsync(Guid userId)
    {
        return patientProfileService.GetUserSettingsAsync(userId);
    }

    /// <summary>
    /// Updates user settings
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
    {
        try
        {
            await patientProfileService.UpdateUserSettingsAsync(
                userId,
                request.DataSharingEnabled,
                request.AiAnalysisEnabled,
                request.ActivityTrackingEnabled
            );
            return (true, "Settings updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error updating settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a support ticket
    /// </summary>
    public async Task<(bool Success, string Message)> CreateSupportTicketAsync(CreateSupportTicketRequest request)
    {
        try
        {
            await patientProfileService.CreateSupportTicketAsync(
                request.UserId,
                request.Subject,
                request.Description,
                request.Category,
                request.Priority
            );
            return (true, "Support ticket created successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error creating support ticket: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all support tickets for a user
    /// </summary>
    public Task<IEnumerable<ISupportTicket>> GetUserSupportTicketsAsync(Guid userId)
    {
        return patientProfileService.GetUserSupportTicketsAsync(userId);
    }

    /// <summary>
    /// Gets a specific support ticket
    /// </summary>
    public Task<ISupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        return patientProfileService.GetSupportTicketAsync(ticketId);
    }
}

/// <summary>
/// Request model for updating patient profile
/// </summary>
public record UpdatePatientProfileRequest(
    string? FullName,
    DateTime? DateOfBirth,
    string? Gender,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BloodType,
    string[]? Allergies,
    string[]? ChronicConditions,
    string[]? CurrentMedications
);

/// <summary>
/// Request model for updating user settings
/// </summary>
public record UpdateUserSettingsRequest(
    bool DataSharingEnabled,
    bool AiAnalysisEnabled,
    bool ActivityTrackingEnabled
);

/// <summary>
/// Request model for creating support ticket
/// </summary>
public record CreateSupportTicketRequest(
    Guid UserId,
    string Subject,
    string Description,
    string? Category,
    string Priority
);
