using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("doctor_profiles")]
public class DoctorProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("title")]
    public string? Title { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("license_number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("primary_specialization")]
    public string PrimarySpecialization { get; set; } = string.Empty;

    [Column("sub_specializations")]
    public string? SubSpecializations { get; set; } // JSON array string

    [Column("medical_expertise_tags")]
    public string? MedicalExpertiseTags { get; set; } // JSON array string

    [Column("symptoms_expertise")]
    public string? SymptomsExpertise { get; set; } // JSON array string

    [Column("conditions_treated")]
    public string? ConditionsTreated { get; set; } // JSON array string

    [Column("procedures_performed")]
    public string? ProceduresPerformed { get; set; } // JSON array string

    [Column("age_groups_treated")]
    public string? AgeGroupsTreated { get; set; } // JSON array string

    [Column("languages_spoken")]
    public string? LanguagesSpoken { get; set; } // JSON array string

    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }

    [Column("availability_status")]
    public DoctorAvailabilityStatus AvailabilityStatus { get; set; } = DoctorAvailabilityStatus.Offline;

    [Column("working_hours")]
    public string? WorkingHours { get; set; } // JSON string

    [Column("current_active_conversations")]
    public int CurrentActiveConversations { get; set; } = 0;

    [Column("total_consultations")]
    public int TotalConsultations { get; set; } = 0;

    [Column("average_rating")]
    public decimal AverageRating { get; set; } = 0.00m;

    [Column("total_ratings")]
    public int TotalRatings { get; set; } = 0;

    [Column("profile_photo_url")]
    public string? ProfilePhotoUrl { get; set; }

    [Column("is_verified")]
    public bool IsVerified { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_accepting_patients")]
    public bool IsAcceptingPatients { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
    
    public ICollection<DoctorRating> Ratings { get; set; } = new List<DoctorRating>();
}
