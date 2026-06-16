using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("conversations")]
public class Conversation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Column("assigned_doctor_id")]
    public Guid? AssignedDoctorId { get; set; }

    [MaxLength(255)]
    [Column("title")]
    public string? Title { get; set; }

    [Column("status")]
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;

    [Column("initial_symptoms")]
    public string? InitialSymptoms { get; set; } // JSON array string

    [MaxLength(255)]
    [Column("ai_suggested_specialization")]
    public string? AiSuggestedSpecialization { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("last_message_at")]
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    [Column("total_messages")]
    public int TotalMessages { get; set; } = 0;

    [Column("ai_messages_count")]
    public int AiMessagesCount { get; set; } = 0;

    [Column("doctor_messages_count")]
    public int DoctorMessagesCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("consultation_status")]
    public string ConsultationStatus { get; set; } = "pending";

    [Column("diagnosis_completed")]
    public bool DiagnosisCompleted { get; set; } = false;

    [Column("prescription_generated")]
    public bool PrescriptionGenerated { get; set; } = false;

    [MaxLength(255)]
    [Column("required_specialization")]
    public string? RequiredSpecialization { get; set; }

    [Column("ai_confidence_score")]
    public decimal? AiConfidenceScore { get; set; }

    [Column("ai_generated_summary")]
    public string? AiGeneratedSummary { get; set; }

    [MaxLength(100)]
    [Column("ai_model_used")]
    public string? AiModelUsed { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;

    [ForeignKey("AssignedDoctorId")]
    public User? AssignedDoctor { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<DoctorRating> Ratings { get; set; } = new List<DoctorRating>();
}
