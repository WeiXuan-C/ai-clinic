using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("activity_logs")]
public class ActivityLog : BaseModel
{
    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    public Guid Id { get; private set; }

    [Column("user_id")]
    public Guid? UserId { get; private set; }

    // Activity Information
    [Column("action")]
    public string Action { get; private set; } = string.Empty;

    [Column("entity_type")]
    public string? EntityType { get; private set; }

    [Column("entity_id")]
    public Guid? EntityId { get; private set; }

    // Request Metadata
    [Column("ip_address")]
    public string? IpAddress { get; private set; }

    [Column("user_agent")]
    public string? UserAgent { get; private set; }

    [Column("details")]
    public Dictionary<string, object>? Details { get; private set; }

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    // Public Methods (Business Logic)
    public void AddDetail(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Detail key cannot be empty");

        Details ??= new Dictionary<string, object>();
        Details[key] = value;
    }

    public object? GetDetail(string key)
    {
        return Details?.ContainsKey(key) == true ? Details[key] : null;
    }

    public bool HasDetails()
    {
        return Details != null && Details.Count > 0;
    }

    // Public method for DAO initialization
    public void Initialize(Guid id, Guid? userId, string action, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty");

        Id = id;
        UserId = userId;
        Action = action;
        CreatedAt = createdAt;
    }

    public void SetRequestMetadata(string? ipAddress, string? userAgent)
    {
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    public void SetEntityInfo(string? entityType, Guid? entityId)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
