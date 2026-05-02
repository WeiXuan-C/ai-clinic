using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("documents")]
public class Document
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    [Required]
    [Column("uploaded_by_user_id")]
    public Guid UploadedByUserId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("file_type")]
    public DocumentType FileType { get; set; }

    [Required]
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    [Required]
    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("mime_type")]
    public string? MimeType { get; set; }

    [Column("is_processed")]
    public bool IsProcessed { get; set; } = false;

    [Column("extracted_text")]
    public string? ExtractedText { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("tags")]
    public string? Tags { get; set; } // JSON array string

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ConversationId")]
    public Conversation Conversation { get; set; } = null!;

    [ForeignKey("UploadedByUserId")]
    public User UploadedByUser { get; set; } = null!;
}
