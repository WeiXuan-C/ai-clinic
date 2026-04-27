using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("support_tickets")]
public class SupportTicket : BaseModel
{
    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    public Guid Id { get; private set; }

    [Column("user_id")]
    public Guid UserId { get; private set; }

    // Ticket Information
    [Column("subject")]
    public string Subject { get; private set; } = string.Empty;

    [Column("description")]
    public string Description { get; private set; } = string.Empty;

    [Column("category")]
    public string? Category { get; private set; }

    [Column("priority")]
    public string Priority { get; private set; } = "medium";

    [Column("status")]
    public string Status { get; private set; } = "open";

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; private set; }

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; private set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; private set; }

    // Public Methods (Business Logic)
    public void UpdateSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty");

        Subject = subject.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");

        Description = description.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriority(string priority)
    {
        if (!IsValidPriority(priority))
            throw new ArgumentException($"Invalid priority: {priority}");

        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCategory(string? category)
    {
        Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartProgress()
    {
        if (Status != "open")
            throw new InvalidOperationException("Only open tickets can be moved to in progress");

        Status = "in_progress";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        if (Status == "resolved" || Status == "closed")
            throw new InvalidOperationException("Ticket is already resolved or closed");

        Status = "resolved";
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = "closed";
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        if (Status == "open")
            throw new InvalidOperationException("Ticket is already open");

        Status = "open";
        ResolvedAt = null;
        ClosedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public TimeSpan? GetResolutionTime()
    {
        if (ResolvedAt.HasValue)
            return ResolvedAt.Value - CreatedAt;

        return null;
    }

    public bool IsOpen()
    {
        return Status == "open" || Status == "in_progress";
    }

    // Private Helper Methods
    private bool IsValidPriority(string priority)
    {
        return priority == "low" || priority == "medium" || priority == "high" || priority == "urgent";
    }

    // Internal method for DAO initialization
    public void Initialize(Guid id, Guid userId, string subject, string description, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");

        Id = id;
        UserId = userId;
        Subject = subject;
        Description = description;
        CreatedAt = createdAt;
    }
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
