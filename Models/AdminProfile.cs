using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("admin_profiles")]
public class AdminProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("manage_users")]
    public bool ManageUsers { get; set; } = false;

    [Column("manage_ai")]
    public bool ManageAi { get; set; } = false;

    [Column("manage_doctors")]
    public bool ManageDoctors { get; set; } = false;

    [Column("manage_tickets")]
    public bool ManageTickets { get; set; } = false;

    [Column("manage_permissions")]
    public bool ManagePermissions { get; set; } = false;

    // Navigation property
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
