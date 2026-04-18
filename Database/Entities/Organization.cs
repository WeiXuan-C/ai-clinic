using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("organizations")]
public class Organization : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("address")]
    public string? Address { get; set; }
    
    [Column("phone")]
    public string? Phone { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("logo_url")]
    public string? LogoUrl { get; set; }
    
    [Column("is_verified")]
    public bool IsVerified { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
