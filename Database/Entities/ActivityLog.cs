using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("activity_logs")]
public class ActivityLog : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid? UserId { get; set; }
    
    [Column("action")]
    public string Action { get; set; } = string.Empty;
    
    [Column("entity_type")]
    public string? EntityType { get; set; }
    
    [Column("entity_id")]
    public Guid? EntityId { get; set; }
    
    [Column("ip_address")]
    public string? IpAddress { get; set; }
    
    [Column("user_agent")]
    public string? UserAgent { get; set; }
    
    [Column("details")]
    public Dictionary<string, object>? Details { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
