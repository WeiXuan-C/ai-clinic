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
