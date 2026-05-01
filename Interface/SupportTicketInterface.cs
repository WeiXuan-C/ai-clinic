using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Support Ticket entity structure (attributes/properties as constraints)
/// </summary>
public interface ISupportTicket
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string Subject { get; set; }
    string Description { get; set; }
    string? Category { get; set; }
    string Priority { get; set; }
    string Status { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    DateTime? ResolvedAt { get; set; }
    DateTime? ClosedAt { get; set; }
}

/// <summary>
/// SupportTicket entity implementation
/// </summary>
[Table("support_tickets")]
public class SupportTicket : BaseModel, ISupportTicket
{
    [PrimaryKey("id", false)]
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
    public DateTime UpdatedAt { get; set; }
    
    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }
    
    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Support Ticket repository operations
/// </summary>
public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id);
    Task<IEnumerable<SupportTicket>> GetAllAsync();
    Task<SupportTicket> AddAsync(SupportTicket entity);
    Task<SupportTicket> UpdateAsync(SupportTicket entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<SupportTicket>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<SupportTicket>> GetByStatusAsync(string status);
}
