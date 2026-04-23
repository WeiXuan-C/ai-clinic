using AiClinic.Core.Entities;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Patient Controller - Facade Pattern
/// Simplifies patient profile operations
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
