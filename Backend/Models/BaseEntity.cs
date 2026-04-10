using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ai_clinic.Backend.Models;

/// <summary>
/// Base class for Supabase entities
/// </summary>
public abstract class BaseEntity : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
