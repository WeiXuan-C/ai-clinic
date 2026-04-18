using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("patient_profiles")]
public class PatientProfile : BaseModel
{
    [PrimaryKey("id")]
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
    public DateTime? UpdatedAt { get; set; }
}
