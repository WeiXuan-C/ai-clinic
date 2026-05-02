using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("doctor_ratings")]
public class DoctorRating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    [Required]
    [Range(1, 5)]
    [Column("rating")]
    public int Rating { get; set; }

    [Column("review_text")]
    public string? ReviewText { get; set; }

    [Range(1, 5)]
    [Column("professionalism_rating")]
    public int? ProfessionalismRating { get; set; }

    [Range(1, 5)]
    [Column("communication_rating")]
    public int? CommunicationRating { get; set; }

    [Range(1, 5)]
    [Column("knowledge_rating")]
    public int? KnowledgeRating { get; set; }

    [Range(1, 5)]
    [Column("response_time_rating")]
    public int? ResponseTimeRating { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DoctorId")]
    public User Doctor { get; set; } = null!;

    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;

    [ForeignKey("ConversationId")]
    public Conversation Conversation { get; set; } = null!;
}
