using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("support_tickets")]
public class SupportTicket : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("user_id")]
    public Guid UserId { get; set; }
    
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("category")]
    public string? Category { get; set; }
    
    [Column("priority")]
    public string Priority { get; set; } = "medium";
    
    [Column("status")]
    public string Status { get; set; } = "open";
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }
    
    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }
}

public enum TicketCategory
{
    Technical,
    Billing,
    Medical,
    Account,
    Other
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum TicketStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}
