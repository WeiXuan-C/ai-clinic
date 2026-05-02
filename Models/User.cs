using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone")]
    public string? Phone { get; set; }

    [Required]
    [Column("role")]
    public UserRole Role { get; set; } = UserRole.Patient;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("data_sharing_enabled")]
    public bool DataSharingEnabled { get; set; } = false;

    [Column("ai_analysis_enabled")]
    public bool AiAnalysisEnabled { get; set; } = true;

    [Column("activity_tracking_enabled")]
    public bool ActivityTrackingEnabled { get; set; } = true;

    [Column("is_deactivated")]
    public bool IsDeactivated { get; set; } = false;

    [Column("deactivated_at")]
    public DateTime? DeactivatedAt { get; set; }

    // Navigation properties
    public PatientProfile? PatientProfile { get; set; }
    public DoctorProfile? DoctorProfile { get; set; }
    public AdminProfile? AdminProfile { get; set; }
    public ICollection<Conversation> PatientConversations { get; set; } = new List<Conversation>();
    public ICollection<Conversation> DoctorConversations { get; set; } = new List<Conversation>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
