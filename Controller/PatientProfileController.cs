using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Patient Controller - Facade Pattern
/// Simplifies patient profile operations, settings, and support tickets
/// </summary>
public class PatientProfileController
{
    private readonly PatientProfileService _patientProfileService;
    private readonly AuthState _authState;

    public PatientProfileController(PatientProfileService PatientProfileService, AuthState authState)
    {
        _patientProfileService = PatientProfileService;
        _authState = authState;
    }

    /// <summary>
    /// Gets patient profile
    /// </summary>
    public async Task<PatientProfile?> GetProfileAsync(Guid userId)
    {
        return await _patientProfileService.GetProfileAsync(userId);
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<(bool Success, string Message, PatientProfile? Profile)> CreateProfileAsync(Guid userId, string fullName)
    {
        try
        {
            var profile = await _patientProfileService.CreateProfileAsync(userId, fullName);
            return (true, "Patient profile created successfully", profile);
        }
        catch (Exception ex)
        {
            return (false, $"Error creating profile: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Updates patient profile
    /// </summary>
    public async Task<PatientProfile> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request)
    {
        // Get existing profile first
        var existingProfile = await _patientProfileService.GetProfileAsync(userId);
        if (existingProfile == null)
        {
            throw new InvalidOperationException("Profile not found");
        }

        // Use factory method to create updated profile
        var updatedProfile = existingProfile.WithUpdatedInfo(
            request.FullName,
            request.DateOfBirth,
            request.Gender,
            request.Address,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.BloodType,
            request.Allergies,
            request.ChronicConditions,
            request.CurrentMedications
        );

        return await _patientProfileService.UpdateProfileAsync(updatedProfile);
    }

    /// <summary>
    /// Checks if email exists
    /// </summary>
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _patientProfileService.CheckEmailExistsAsync(email);
    }

    /// <summary>
    /// Gets user settings
    /// </summary>
    public async Task<User?> GetUserSettingsAsync(Guid userId)
    {
        return await _patientProfileService.GetUserSettingsAsync(userId);
    }

    /// <summary>
    /// Updates user settings
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
    {
        try
        {
            var user = await _patientProfileService.GetUserSettingsAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            var updatedUser = user.WithUpdatedPrivacySettings(
                request.DataSharingEnabled,
                request.AiAnalysisEnabled,
                request.ActivityTrackingEnabled
            );

            await _patientProfileService.UpdateUserSettingsAsync(updatedUser);
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
    public async Task<(bool Success, string Message, SupportTicket? Ticket)> CreateSupportTicketAsync(CreateSupportTicketRequest request)
    {
        try
        {
            // Create ticket using Initialize method (respects private setters)
            var ticket = new SupportTicket();
            ticket.Initialize(
                id: Guid.NewGuid(),
                userId: request.UserId,
                subject: request.Subject,
                description: request.Description,
                createdAt: DateTime.UtcNow
            );
            
            // Set optional properties through business methods
            if (!string.IsNullOrEmpty(request.Category))
            {
                ticket.SetCategory(request.Category);
            }
            
            if (!string.IsNullOrEmpty(request.Priority))
            {
                ticket.SetPriority(request.Priority);
            }

            var createdTicket = await _patientProfileService.CreateSupportTicketAsync(ticket);
            return (true, "Support ticket created successfully", createdTicket);
        }
        catch (Exception ex)
        {
            return (false, $"Error creating support ticket: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Gets all support tickets for a user
    /// </summary>
    public async Task<IEnumerable<SupportTicket>> GetUserSupportTicketsAsync(Guid userId)
    {
        return await _patientProfileService.GetUserSupportTicketsAsync(userId);
    }

    /// <summary>
    /// Gets a specific support ticket
    /// </summary>
    public async Task<SupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        return await _patientProfileService.GetSupportTicketAsync(ticketId);
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
