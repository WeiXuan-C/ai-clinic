using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Interfaces;

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
public class SupportTicket : BaseModel, ISupportTicket
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
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
