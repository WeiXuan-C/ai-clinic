using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("user_suspensions")]
public class UserSuspension
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("suspended_by_admin_id")]
    public Guid SuspendedByAdminId { get; set; }

    [Required]
    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("suspension_start")]
    public DateTime SuspensionStart { get; set; } = DateTime.UtcNow;

    [Column("suspension_end")]
    public DateTime? SuspensionEnd { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("lifted_at")]
    public DateTime? LiftedAt { get; set; }

    [Column("lifted_by_admin_id")]
    public Guid? LiftedByAdminId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    [ForeignKey("SuspendedByAdminId")]
    public User SuspendedByAdmin { get; set; } = null!;

    [ForeignKey("LiftedByAdminId")]
    public User? LiftedByAdmin { get; set; }
}
