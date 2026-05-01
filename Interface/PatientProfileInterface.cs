using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

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
/// PatientProfile entity implementation
/// </summary>
[Table("patient_profiles")]
public class PatientProfile : BaseModel, IPatientProfile
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("full_name")]
    public string? FullName { get; set; }
    
    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }
    
    [Column("gender")]
    public string? Gender { get; set; }
    
    [Column("address")]
    public string? Address { get; set; }
    
    [Column("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }
    
    [Column("emergency_contact_phone")]
    public string? EmergencyContactPhone { get; set; }
    
    [Column("blood_type")]
    public string? BloodType { get; set; }
    
    [Column("allergies")]
    public string[]? Allergies { get; set; }
    
    [Column("chronic_conditions")]
    public string[]? ChronicConditions { get; set; }
    
    [Column("current_medications")]
    public string[]? CurrentMedications { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
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
