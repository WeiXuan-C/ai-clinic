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
}

/// <summary>
/// Doctor entity implementation
/// </summary>
public class Doctor : BaseModel, IDoctorProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string PrimarySpecialization { get; set; } = string.Empty;
    public string[]? SubSpecializations { get; set; }
    public string[]? MedicalExpertiseTags { get; set; }
    public string[]? SymptomsExpertise { get; set; }
    public string[]? ConditionsTreated { get; set; }
    public string[]? ProceduresPerformed { get; set; }
    public string[]? AgeGroupsTreated { get; set; }
    public string[]? LanguagesSpoken { get; set; }
    public int? YearsOfExperience { get; set; }
    public string AvailabilityStatus { get; set; } = "available";
    public object? WorkingHours { get; set; }
    public int CurrentActiveConversations { get; set; }
    public int TotalConsultations { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAcceptingPatients { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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
