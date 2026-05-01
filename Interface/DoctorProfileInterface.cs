using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Doctor Profile entity structure (attributes/properties as constraints)
/// </summary>
public interface IDoctorProfile
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string FullName { get; set; }
    string? Title { get; set; }
    string LicenseNumber { get; set; }
    string PrimarySpecialization { get; set; }
    string[]? SubSpecializations { get; set; }
    string[]? MedicalExpertiseTags { get; set; }
    string[]? SymptomsExpertise { get; set; }
    string[]? ConditionsTreated { get; set; }
    string[]? ProceduresPerformed { get; set; }
    string[]? AgeGroupsTreated { get; set; }
    string[]? LanguagesSpoken { get; set; }
    int? YearsOfExperience { get; set; }
    string AvailabilityStatus { get; set; }
    object? WorkingHours { get; set; }
    int CurrentActiveConversations { get; set; }
    int TotalConsultations { get; set; }
    decimal AverageRating { get; set; }
    int TotalRatings { get; set; }
    string? ProfilePhotoUrl { get; set; }
    bool IsVerified { get; set; }
    bool IsActive { get; set; }
    bool IsAcceptingPatients { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    Guid? MVerifiedByAdminId { get; set; }
    string? VerificationStatus { get; set; }
    string? VerificationNotes { get; set; }
    object? DocumentsChecked { get; set; }
    DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// Doctor entity implementation
/// </summary>
[Table("doctor_profiles")]
public class Doctor : BaseModel, IDoctorProfile
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [Column("title")]
    public string? Title { get; set; }
    
    [Column("license_number")]
    public string LicenseNumber { get; set; } = string.Empty;
    
    [Column("primary_specialization")]
    public string PrimarySpecialization { get; set; } = string.Empty;
    
    [Column("sub_specializations")]
    public string[]? SubSpecializations { get; set; }
    
    [Column("medical_expertise_tags")]
    public string[]? MedicalExpertiseTags { get; set; }
    
    [Column("symptoms_expertise")]
    public string[]? SymptomsExpertise { get; set; }
    
    [Column("conditions_treated")]
    public string[]? ConditionsTreated { get; set; }
    
    [Column("procedures_performed")]
    public string[]? ProceduresPerformed { get; set; }
    
    [Column("age_groups_treated")]
    public string[]? AgeGroupsTreated { get; set; }
    
    [Column("languages_spoken")]
    public string[]? LanguagesSpoken { get; set; }
    
    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }
    
    [Column("availability_status")]
    public string AvailabilityStatus { get; set; } = "available";
    
    [Column("working_hours")]
    public object? WorkingHours { get; set; }
    
    [Column("current_active_conversations")]
    public int CurrentActiveConversations { get; set; }
    
    [Column("total_consultations")]
    public int TotalConsultations { get; set; }
    
    [Column("average_rating")]
    public decimal AverageRating { get; set; }
    
    [Column("total_ratings")]
    public int TotalRatings { get; set; }
    
    [Column("profile_photo_url")]
    public string? ProfilePhotoUrl { get; set; }
    
    [Column("is_verified")]
    public bool IsVerified { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("is_accepting_patients")]
    public bool IsAcceptingPatients { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("mverified_by_admin_id")]
    public Guid? MVerifiedByAdminId { get; set; }
    
    [Column("verification_status")]
    public string? VerificationStatus { get; set; }
    
    [Column("verification_notes")]
    public string? VerificationNotes { get; set; }
    
    [Column("documents_checked")]
    public object? DocumentsChecked { get; set; }
    
    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Doctor Profile repository operations
/// </summary>
public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id);
    Task<IEnumerable<Doctor>> GetAllAsync();
    Task<Doctor> AddAsync(Doctor entity);
    Task<Doctor> UpdateAsync(Doctor entity);
    Task<bool> DeleteAsync(Guid id);
    Task<Doctor?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync();
    Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);
    Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId);
    Task UpdateAvailabilityStatusAsync(Guid doctorId, string status);
}
