using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("patient_profiles")]
public class PatientProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [MaxLength(255)]
    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    [Column("gender")]
    public string? Gender { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(255)]
    [Column("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    [Column("emergency_contact_phone")]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(5)]
    [Column("blood_type")]
    public string? BloodType { get; set; }

    [Column("allergies")]
    public string? Allergies { get; set; } // Stored as JSON array string

    [Column("chronic_conditions")]
    public string? ChronicConditions { get; set; } // Stored as JSON array string

    [Column("current_medications")]
    public string? CurrentMedications { get; set; } // Stored as JSON array string

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
