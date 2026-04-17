namespace AiClinic.Application.DTOs;

public record PatientProfileDto(
    Guid Id,
    Guid UserId,
    string? FullName,
    DateTime? DateOfBirth,
    string? Gender,
    string? Address,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? BloodType,
    string[]? Allergies,
    string[]? ChronicConditions,
    string[]? CurrentMedications,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreatePatientProfileRequest(
    Guid UserId,
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

public record ActivityLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    Dictionary<string, object>? Details,
    DateTime CreatedAt
);
