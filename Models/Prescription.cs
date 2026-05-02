using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("prescriptions")]
public class Prescription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("consultation_note_id")]
    public Guid? ConsultationNoteId { get; set; }

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("medication_name")]
    public string MedicationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("dosage")]
    public string Dosage { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("duration")]
    public string? Duration { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ConsultationNoteId")]
    public ConsultationNote? ConsultationNote { get; set; }

    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public User Doctor { get; set; } = null!;
}
