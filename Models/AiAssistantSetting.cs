using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("ai_assistant_settings")]
public class AiAssistantSetting
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("model_name")]
    public string ModelName { get; set; } = string.Empty;

    [Required]
    [Column("model_type")]
    public AiModelType ModelType { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_available_for_patients")]
    public bool IsAvailableForPatients { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("enable_document_analysis")]
    public bool EnableDocumentAnalysis { get; set; } = true;

    [Column("enable_symptom_checker")]
    public bool EnableSymptomChecker { get; set; } = true;

    [Column("enable_doctor_recommendation")]
    public bool EnableDoctorRecommendation { get; set; } = true;

    [Column("created_by_admin_id")]
    public Guid? CreatedByAdminId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("CreatedByAdminId")]
    public User? CreatedByAdmin { get; set; }
}
