using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("support_ticket_attachments")]
public class SupportTicketAttachment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [Required]
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    [MaxLength(100)]
    [Column("mime_type")]
    public string? MimeType { get; set; }

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("TicketId")]
    public SupportTicket Ticket { get; set; } = null!;
}
