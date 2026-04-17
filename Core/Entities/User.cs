using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Column("phone")]
    public string? PhoneNumber { get; set; }
    
    [Column("role")]
    public string Role { get; set; } = "patient";
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
    
    // Privacy settings
    [Column("data_sharing_enabled")]
    public bool DataSharingEnabled { get; set; } = false;
    
    [Column("ai_analysis_enabled")]
    public bool AiAnalysisEnabled { get; set; } = true;
    
    [Column("activity_tracking_enabled")]
    public bool ActivityTrackingEnabled { get; set; } = true;
    
    // Account status
    [Column("is_deactivated")]
    public bool IsDeactivated { get; set; } = false;
    
    [Column("deactivated_at")]
    public DateTime? DeactivatedAt { get; set; }
}

public enum UserRole
{
    Patient,
    Doctor,
    Admin
}
