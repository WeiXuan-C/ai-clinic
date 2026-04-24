using AiClinic.Core.Entities;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Patient Controller - Facade Pattern
/// Simplifies patient profile operations, settings, and support tickets
/// </summary>
public class PatientController
{
    private readonly PatientService _patientService;
    private readonly AuthState _authState;

    public PatientController(PatientService patientService, AuthState authState)
    {
        _patientService = patientService;
        _authState = authState;
    }

    /// <summary>
    /// Gets patient profile
    /// </summary>
    public async Task<PatientProfile?> GetProfileAsync(Guid userId)
    {
        return await _patientService.GetProfileAsync(userId);
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<(bool Success, string Message, PatientProfile? Profile)> CreateProfileAsync(Guid userId, string fullName)
    {
        try
        {
            var profile = await _patientService.CreateProfileAsync(userId, fullName);
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
        var profile = new PatientProfile
        {
            UserId = userId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            BloodType = request.BloodType,
            Allergies = request.Allergies,
            ChronicConditions = request.ChronicConditions,
            CurrentMedications = request.CurrentMedications,
            UpdatedAt = DateTime.UtcNow
        };

        return await _patientService.UpdateProfileAsync(userId, profile);
    }

    /// <summary>
    /// Checks if email exists
    /// </summary>
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _patientService.CheckEmailExistsAsync(email);
    }

    /// <summary>
    /// Gets user settings
    /// </summary>
    public async Task<User?> GetUserSettingsAsync(Guid userId)
    {
        return await _patientService.GetUserSettingsAsync(userId);
    }

    /// <summary>
    /// Updates user settings
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateUserSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
    {
        try
        {
            var user = await _patientService.GetUserSettingsAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            user.DataSharingEnabled = request.DataSharingEnabled;
            user.AiAnalysisEnabled = request.AiAnalysisEnabled;
            user.ActivityTrackingEnabled = request.ActivityTrackingEnabled;
            user.UpdatedAt = DateTime.UtcNow;

            await _patientService.UpdateUserSettingsAsync(user);
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
            var ticket = new SupportTicket
            {
                UserId = request.UserId,
                Subject = request.Subject,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            var createdTicket = await _patientService.CreateSupportTicketAsync(ticket);
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
        return await _patientService.GetUserSupportTicketsAsync(userId);
    }

    /// <summary>
    /// Gets a specific support ticket
    /// </summary>
    public async Task<SupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        return await _patientService.GetSupportTicketAsync(ticketId);
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
