using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("medical_records")]
public class MedicalRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("conversation_id")]
    public Guid? ConversationId { get; set; }

    [Column("created_by_doctor_id")]
    public Guid? CreatedByDoctorId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("record_type")]
    public string RecordType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("diagnosis_code")]
    public string? DiagnosisCode { get; set; }

    [Column("diagnosis_description")]
    public string? DiagnosisDescription { get; set; }

    [Column("medications")]
    public string? Medications { get; set; } // JSON string

    [Column("record_date")]
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;

    [Column("is_exported")]
    public bool IsExported { get; set; } = false;

    [Column("export_count")]
    public int ExportCount { get; set; } = 0;

    [Column("last_exported_at")]
    public DateTime? LastExportedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;

    [ForeignKey("ConversationId")]
    public Conversation? Conversation { get; set; }

    [ForeignKey("CreatedByDoctorId")]
    public User? CreatedByDoctor { get; set; }
}
