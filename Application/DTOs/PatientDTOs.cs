namespace AiClinic.Application.DTOs;

public record PatientProfileDto(
    Guid Id,
    Guid UserId,
    string? FullName,
    DateTime? DateOfBirth,
    string? Gender,
    string? Address,
    string? BloodType,
    string[]? Allergies,
    string[]? ChronicConditions,
    string[]? CurrentMedications
);

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
