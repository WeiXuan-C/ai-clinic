using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("support_ticket_responses")]
public class SupportTicketResponse
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Required]
    [Column("responder_id")]
    public Guid ResponderId { get; set; }

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("is_internal_note")]
    public bool IsInternalNote { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TicketId")]
    public SupportTicket Ticket { get; set; } = null!;

    [ForeignKey("ResponderId")]
    public User Responder { get; set; } = null!;
}
