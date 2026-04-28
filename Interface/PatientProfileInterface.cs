namespace AiClinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Patient Profile entity structure (attributes/properties as constraints)
/// </summary>
public interface IPatientProfile
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string? FullName { get; set; }
    DateTime? DateOfBirth { get; set; }
    string? Gender { get; set; }
    string? Address { get; set; }
    string? EmergencyContactName { get; set; }
    string? EmergencyContactPhone { get; set; }
    string? BloodType { get; set; }
    string[]? Allergies { get; set; }
    string[]? ChronicConditions { get; set; }
    string[]? CurrentMedications { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Patient Profile repository operations
/// </summary>
public interface IPatientProfileRepository
{
    Task<PatientProfile?> GetByIdAsync(Guid id);
    Task<IEnumerable<PatientProfile>> GetAllAsync();
    Task<PatientProfile> AddAsync(PatientProfile entity);
    Task<PatientProfile> UpdateAsync(PatientProfile entity);
    Task<bool> DeleteAsync(Guid id);
    Task<PatientProfile?> GetByUserIdAsync(Guid userId);
}
