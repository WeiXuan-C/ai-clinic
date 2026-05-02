using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("consultation_notes")]
public class ConsultationNote
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("symptoms")]
    public string? Symptoms { get; set; } // JSON array string

    [Column("physical_examination")]
    public string? PhysicalExamination { get; set; }

    [Required]
    [Column("diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;

    [Column("treatment_plan")]
    public string? TreatmentPlan { get; set; }

    [Column("follow_up_instructions")]
    public string? FollowUpInstructions { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Column("is_finalized")]
    public bool IsFinalized { get; set; } = false;

    [Column("finalized_at")]
    public DateTime? FinalizedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ConversationId")]
    public Conversation Conversation { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public User Doctor { get; set; } = null!;

    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;

    [ForeignKey("PrescriptionId")]
    public MedicalRecord? Prescription { get; set; }

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
