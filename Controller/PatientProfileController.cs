using AiClinic.Interfaces;
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
        var profile = await _patientProfileService.GetProfileAsync(userId);
        return profile as PatientProfile;
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<(bool Success, string Message, PatientProfile? Profile)> CreateProfileAsync(Guid userId, string fullName)
    {
        try
        {
            var profile = await _patientProfileService.CreateProfileAsync(userId, fullName);
            return (true, "Patient profile created successfully", profile as PatientProfile);
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

        // Create updated profile with new values
        var updatedProfile = new PatientProfile
        {
            Id = existingProfile.Id,
            UserId = existingProfile.UserId,
            FullName = request.FullName ?? existingProfile.FullName,
            DateOfBirth = request.DateOfBirth ?? existingProfile.DateOfBirth,
            Gender = request.Gender ?? existingProfile.Gender,
            Address = request.Address ?? existingProfile.Address,
            EmergencyContactName = request.EmergencyContactName ?? existingProfile.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone ?? existingProfile.EmergencyContactPhone,
            BloodType = request.BloodType ?? existingProfile.BloodType,
            Allergies = request.Allergies ?? existingProfile.Allergies,
            ChronicConditions = request.ChronicConditions ?? existingProfile.ChronicConditions,
            CurrentMedications = request.CurrentMedications ?? existingProfile.CurrentMedications,
            CreatedAt = existingProfile.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _patientProfileService.UpdateProfileAsync(updatedProfile);
        return result as PatientProfile ?? throw new InvalidOperationException("Update failed");
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
        var settings = await _patientProfileService.GetUserSettingsAsync(userId);
        return settings as User;
    }

    /// <summary>
    /// Updates user settings
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
    {
        try
        {
            var settings = new
            {
                DataSharingEnabled = request.DataSharingEnabled,
                AiAnalysisEnabled = request.AiAnalysisEnabled,
                ActivityTrackingEnabled = request.ActivityTrackingEnabled
            };

            await _patientProfileService.UpdateUserSettingsAsync(userId, settings);
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
            // Create ticket with all properties
            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Subject = request.Subject,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority ?? "medium",
                Status = "open",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdTicket = await _patientProfileService.CreateSupportTicketAsync(ticket);
            return (true, "Support ticket created successfully", createdTicket as SupportTicket);
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
        var tickets = await _patientProfileService.GetUserSupportTicketsAsync(userId);
        return tickets.Cast<SupportTicket>();
    }

    /// <summary>
    /// Gets a specific support ticket
    /// </summary>
    public async Task<SupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        var ticket = await _patientProfileService.GetSupportTicketAsync(ticketId);
        return ticket as SupportTicket;
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
