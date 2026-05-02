using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("support_tickets")]
public class SupportTicket
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

    [MaxLength(50)]
    [Column("priority")]
    public string Priority { get; set; } = "medium";

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "open";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    public ICollection<SupportTicketAttachment> Attachments { get; set; } = new List<SupportTicketAttachment>();
    public ICollection<SupportTicketResponse> Responses { get; set; } = new List<SupportTicketResponse>();
}
