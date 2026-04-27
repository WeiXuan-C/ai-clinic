using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("organizations")]
public class Organization : BaseModel
{
    // Primary Key
    [PrimaryKey("id")]
    public Guid Id { get; private set; }

    // Basic Information
    [Column("name")]
    public string Name { get; private set; } = string.Empty;

    [Column("description")]
    public string? Description { get; private set; }

    [Column("address")]
    public string? Address { get; private set; }

    [Column("phone")]
    public string? Phone { get; private set; }

    [Column("email")]
    public string? Email { get; private set; }

    [Column("logo_url")]
    public string? LogoUrl { get; private set; }

    // Status
    [Column("is_verified")]
    public bool IsVerified { get; private set; }

    [Column("is_active")]
    public bool IsActive { get; private set; } = true;

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; private set; }

    // Public Methods (Business Logic)
    public void UpdateBasicInfo(string name, string? description, string? address, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty");

        Name = name.Trim();
        Description = description?.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Email = email?.ToLower().Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLogo(string logoUrl)
    {
        LogoUrl = logoUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // Public method for DAO initialization
    public void Initialize(Guid id, string name, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be empty");

        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }
}
