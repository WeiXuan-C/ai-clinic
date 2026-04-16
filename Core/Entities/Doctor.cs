using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("doctor_profiles")]
public class Doctor : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }
    
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
    
    [Column("availability_status")]
    public string AvailabilityStatus { get; set; } = "offline";
    
    [Column("current_active_conversations")]
    public int CurrentActiveConversations { get; set; }
    
    [Column("average_rating")]
    public decimal AverageRating { get; set; }
    
    [Column("total_consultations")]
    public int TotalConsultations { get; set; }
    
    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }
    
    [Column("bio")]
    public string? Bio { get; set; }
    
    [Column("is_verified")]
    public bool IsVerified { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public enum DoctorStatus
{
    Available,
    Busy,
    Offline
}
