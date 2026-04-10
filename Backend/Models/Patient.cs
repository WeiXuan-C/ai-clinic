using Supabase.Postgrest.Attributes;

namespace ai_clinic.Backend.Models;

/// <summary>
/// Example Patient model for Supabase
/// </summary>
[Table("patients")]
public class Patient : BaseEntity
{
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }
}
